#!/usr/bin/env bash
# CPO: gộp nhánh task/T-### vào nhánh sprint, chỉ khi check xanh.
# Dùng: bash scripts/merge-task.sh T-123 [sprint/n]
# - Không ghi nhánh sprint: lấy nhánh sprint/<n> có số lớn nhất.
# - Fast-forward nếu sprint là tổ tiên của task; nếu không thì merge sprint vào task trong worktree của task.
# - Có package.json: chạy npm ci khi package-lock.json khác lần cài cuối; chạy npm run check nếu có script "check".
#   Đỏ thì dừng, không đổi nhánh sprint. Không có package.json thì chỉ chạy kiểm BACKLOG.
set -euo pipefail
task="${1:?Thiếu mã task, ví dụ T-123}"
sprint="${2:-}"
if [ -z "$sprint" ]; then
  sprint="$(git for-each-ref --format='%(refname:short)' 'refs/heads/sprint/*' | sort -t/ -k2 -n | tail -1)"
  [ -n "$sprint" ] || { echo "Không có nhánh sprint/<n>; truyền tên nhánh ở đối số 2"; exit 1; }
fi
root="$(git rev-parse --show-toplevel)"
branch="task/$task"
git rev-parse --verify --quiet "refs/heads/$branch" >/dev/null || { echo "Không có nhánh $branch"; exit 1; }
git rev-parse --verify --quiet "refs/heads/$sprint" >/dev/null || { echo "Không có nhánh $sprint"; exit 1; }
wt="$(git worktree list --porcelain | awk -v b="refs/heads/$branch" '$1=="worktree"{p=substr($0,10)} $1=="branch"&&$2==b{print p}')"
if git merge-base --is-ancestor "$sprint" "$branch"; then
  echo "Fast-forward $sprint -> $branch"
else
  [ -n "$wt" ] || { echo "Không thấy worktree của $branch để merge"; exit 1; }
  [ -z "$(git -C "$wt" status --porcelain)" ] || { echo "Worktree $wt chưa sạch"; exit 1; }
  git -C "$wt" merge --no-edit "$sprint"
fi
dir="${wt:-$root}"
log="${TMPDIR:-/tmp}/merge-$task.log"
if [ -n "$wt" ]; then
  if [ -f "$dir/package.json" ]; then
    if [ -f "$dir/package-lock.json" ] && ! cmp -s "$dir/package-lock.json" "$dir/node_modules/.package-lock.json" 2>/dev/null; then
      echo "package-lock đổi: npm ci"; (cd "$dir" && npm ci --silent)
    fi
    if (cd "$dir" && node -e "process.exit(require('./package.json').scripts?.check ? 0 : 1)"); then
      (cd "$dir" && npm run --silent check) > "$log" 2>&1 || {
        echo "CHECK ĐỎ, không merge. Log: $log"; grep -E "Tests |Error|error" "$log" | head -5; exit 1; }
      grep -E "Tests " "$log" || echo "check xanh"
    fi
  fi
  if [ -f "$dir/scripts/backlog-check.mjs" ]; then
    (cd "$dir" && node scripts/backlog-check.mjs) || { echo "BACKLOG lỗi trên $branch, không merge"; exit 1; }
  fi
fi
swt="$(git worktree list --porcelain | awk -v b="refs/heads/$sprint" '$1=="worktree"{p=substr($0,10)} $1=="branch"&&$2==b{print p}')"
if [ -n "$swt" ]; then git -C "$swt" merge --ff-only "$branch"; else git branch -f "$sprint" "$branch"; fi
git log --oneline -1 "$sprint"
