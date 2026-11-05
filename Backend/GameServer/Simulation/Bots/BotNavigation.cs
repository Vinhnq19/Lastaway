using System.Diagnostics;
using System.Numerics;
using JungleMaze.World;

namespace GameServer.Simulation.Bots;

/// <summary>Immutable static-cover grid, constructed with the map outside the simulation loop.</summary>
public sealed class BotNavigationGrid
{
    public const float Step = .5f;
    public int Width { get; }
    private readonly WorldObjectDefinition[] _cover;
    public BotNavigationGrid(MapLayout layout)
    {
        Width = checked(layout.Size * 2);
        _cover = layout.Objects.Where(o => o.IsHardCover).ToArray();
    }
    public Vector2 Point(int id) => new((id % Width + .5f) * Step, (id / Width + .5f) * Step);
    public int Cell(Vector2 point) => Math.Clamp((int)(point.Y / Step), 0, Width - 1) * Width + Math.Clamp((int)(point.X / Step), 0, Width - 1);
    public bool Walkable(int id, float radius)
    {
        var p = Point(id);
        if (p.X < radius || p.Y < radius || p.X > Width * Step - radius || p.Y > Width * Step - radius) return false;
        foreach (var o in _cover) if (o.Bounds.IntersectsCircle(new WorldPoint(p.X, p.Y), radius)) return false;
        return true;
    }
}

public sealed class BotNavigationBudget
{
    public int Remaining { get; private set; }
    public int Used { get; private set; }
    internal bool Shared;
    private long _deadline;
    public bool CanWork => Remaining > 0 && (_deadline == 0 || Stopwatch.GetTimestamp() < _deadline);
    // Zero disables the wall-time limit for deterministic algorithm tests; production supplies BotSettings.
    public void Reset(int limit, double milliseconds = 0)
    {
        Remaining = limit; Used = 0;
        _deadline = milliseconds > 0 ? Stopwatch.GetTimestamp() + Math.Max(1, (long)(Stopwatch.Frequency * milliseconds / 1000)) : 0;
    }
    public bool Take()
    {
        if (!CanWork) return false;
        Remaining--; Used++; return true;
    }
}

/// <summary>One resumable bounded A* job. No Task, waiting, or unbounded search on the tick thread.</summary>
public sealed class BotPathSearch
{
    private readonly BotNavigationGrid _grid;
    private int _goal;
    private readonly Vector2 _destination, _start;
    private bool _initialised;
    private int _initialisationIndex, _bestGoal = -1, _bestStart = -1;
    private float _goalDistance = float.MaxValue, _startDistance = float.MaxValue;
    private readonly PriorityQueue<int, float> _open = new();
    private readonly Dictionary<int, float> _cost = new();
    private readonly Dictionary<int, int> _parent = new();
    private readonly HashSet<int> _closed = new();
    private readonly List<Vector2> _path = new();
    public bool Finished { get; private set; }
    public bool ReachedGoal { get; private set; }
    public int Expanded { get; private set; }
    public IReadOnlyList<Vector2> Path => _path;
    public BotPathSearch(BotNavigationGrid grid, Vector2 start, Vector2 goal)
    {
        _grid = grid; _destination = goal; _start = start; _goal = grid.Cell(goal);
    }
    public void Advance(BotNavigationBudget budget, int requestLimit, float radius,
        Func<Vector2, bool> walkable, Func<Vector2, Vector2, bool> segment, Func<Vector2, float> penalty)
    {
        if (!_initialised)
        {
            // The 50 endpoint probes are work too: retain progress instead of doing all of them in one tick.
            while (_initialisationIndex < 50)
            {
                if (!budget.CanWork) return;
                var index = _initialisationIndex++;
                var origin = index % 2 == 0 ? _destination : _start;
                var dx = index / 2 % 5 - 2; var dy = index / 10 - 2;
                var cx = (int)(origin.X / BotNavigationGrid.Step) + dx; var cy = (int)(origin.Y / BotNavigationGrid.Step) + dy;
                if (cx < 0 || cy < 0 || cx >= _grid.Width || cy >= _grid.Width) continue;
                var id = cy * _grid.Width + cx; var point = _grid.Point(id);
                if (!_grid.Walkable(id, radius) || !walkable(point) || !segment(origin, point)) continue;
                var distance = Vector2.DistanceSquared(point, origin);
                if (origin == _destination && distance < _goalDistance) { _bestGoal = id; _goalDistance = distance; }
                if (origin == _start && distance < _startDistance) { _bestStart = id; _startDistance = distance; }
            }
            _initialised = true;
            if (_bestStart < 0 || _bestGoal < 0) { Finished = true; return; }
            _goal = _bestGoal; _cost[_bestStart] = 0; _open.Enqueue(_bestStart, 0);
        }
        while (!Finished && _open.Count > 0 && budget.CanWork)
        {
            var current = _open.Peek();
            if (_closed.Contains(current)) { _open.Dequeue(); continue; }
            // Do not consume the frontier node until both shared budgets allow processing it.
            if (!budget.Take()) return;
            _open.Dequeue(); _closed.Add(current);
            Expanded++;
            if (current == _goal) { ReachedGoal = true; Complete(current); return; }
            var p = _grid.Point(current); var cx = current % _grid.Width; var cy = current / _grid.Width;
            for (var dy = -1; dy <= 1; dy++) for (var dx = -1; dx <= 1; dx++)
            {
                if ((dx == 0 && dy == 0) || cx + dx < 0 || cy + dy < 0 || cx + dx >= _grid.Width || cy + dy >= _grid.Width) continue;
                var next = (cy + dy) * _grid.Width + cx + dx;
                if (_closed.Contains(next) || !_grid.Walkable(next, radius)) continue;
                var q = _grid.Point(next);
                if (!walkable(q) || !segment(p, q)) continue;
                var cost = _cost[current] + Vector2.Distance(p, q) * Math.Max(1, penalty(q));
                if (_cost.TryGetValue(next, out var old) && old <= cost) continue;
                _cost[next] = cost; _parent[next] = current;
                _open.Enqueue(next, cost + Vector2.Distance(q, _grid.Point(_goal)));
            }
            if (Expanded >= requestLimit) { Finished = true; return; }
        }
        if (_open.Count == 0) Finished = true;
    }
    private void Complete(int id)
    {
        Finished = true;
        while (_parent.TryGetValue(id, out var parent)) { _path.Add(_grid.Point(id)); id = parent; }
        _path.Reverse();
    }
}
