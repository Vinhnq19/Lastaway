import sys, pathlib
from PIL import Image, ImageDraw
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from mock import F, OUT, GOLD
WF = pathlib.Path(r"D:\Lastaway\Docs\Wireframes"); UX = pathlib.Path(r"D:\Lastaway\Docs\UX\Round2_Captures")
PAIRS = {  # mock -> (wireframe file, nhan)
 "a/a1-khoi-dong": (WF / "13-2_boot_screen.png", "Wireframe 13:2 (frame trống trong Figma)"),
 "a/a2-sanh-chinh": (UX / "03_home.png", "Figma vòng 2, 03_home (Round2_Captures)"),
 "a/a3-vao-phong": (UX / "06_join-modal.png", "Figma vòng 2, 06_join-modal"),
 "a/a3b-dang-nhap": (UX / "01_login.png", "Figma vòng 2, 01_login"),
 "a/a4-phong-cua-toi": (WF / "8-7_find_room.png", "Wireframe 8:7 Tìm phòng (cũ, 100 người)"),
 "a/a5-tao-phong": (WF / "8-124_create_room.png", "Wireframe 8:124 Tạo phòng (cũ, có Luật chơi)"),
 "a/a6-phong-cho-dem-nguoc": (WF / "v2" / "42-182_flow03_lobby.png", "Figma v2 42:182 Phòng chờ"),
 "a/a7-dang-tai": (WF / "v2" / "42-269_flow04_loading.png", "Figma v2 42:269 Đang tải"),
 "b/b1-esc-cai-dat": (WF / "7-337_settings.png", "Wireframe 7:337 Cài đặt (v1, 5 tab, bỏ theo UI-03)"),
 "b/b2-bi-loai": (WF / "v2" / "43-283_flow06_eliminated.png", "Figma v2 43:283 Bị loại"),
 "b/b3-spectator": (WF / "7-304_spectate_teammate.png", "Wireframe 7:304 Theo dõi (v1, có đội, Báo cáo)"),
 "b/b4-tui-do": (WF / "3-3_inventory.png", "Wireframe 3:3 Balo (PUBG, phụ kiện súng)"),
 "b/b5-ban-do-lon": (WF / "7-186_big_map.png", "Wireframe 7:186 Bản đồ lớn"),
 "b/b6-thang": (WF / "v2" / "43-445_flow07_result.png", "Figma v2 43:445 Kết quả"),
 "b/b7-thua": (WF / "3-5_lose.png", "Wireframe 3:5 Thua (v1)"),
 "b/b8-mat-ket-noi": (WF / "v2" / "43-131_flow05_reconnect.png", "Figma v2 43:131 Kết nối lại"),
 "b/b9-phan-hoi-hud": (WF / "v2" / "48-461_hud_feedback_annotated.png", "Figma v2 48:461 Phản hồi HUD"),
}
def make(mock, wf, lab):
    a = Image.open(wf).convert("RGB").resize((960, 540), Image.LANCZOS); b = Image.open(OUT / f"{mock}.png").convert("RGB").resize((960, 540), Image.LANCZOS)
    o = Image.new("RGB", (1940, 590), (20, 20, 20)); o.paste(a, (0, 50)); o.paste(b, (980, 50)); d = ImageDraw.Draw(o); d.text((10, 10), lab, font=F(22), fill=GOLD); d.text((990, 10), "Mock skin A (UI kit T-020)", font=F(22), fill=GOLD)
    name = mock.split("/")[-1] + "-canh-wireframe.png"; o.save(OUT / mock.split("/")[0] / name); return name
if __name__ == "__main__":
    for m, (w, l) in PAIRS.items(): print(make(m, w, l))
