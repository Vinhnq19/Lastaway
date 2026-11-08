"""T-026: nguon du lieu duy nhat cho bang doi chieu va anh thieu-gi.png. Chay: PYTHONIOENCODING=utf-8 python thieu.py
Ra: ../thieu-gi.png, ../bang-doi-chieu.md (dan vao de-xuat), in so dem."""
import sys, pathlib, json
from PIL import Image, ImageDraw, ImageFont
T20 = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-020"); T15 = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015"); OUT = pathlib.Path(__file__).resolve().parent.parent
FB = r"D:\Lastaway\Assets\_JungleMaze\Art\Fonts\BeVietnamPro-Bold.ttf"; F = lambda n: ImageFont.truetype(FB, n)

# (ma, nhom, ten, sprite mau [(root, ten)], man hinh, ghi chu)
CO = [
 ("C01", "Nút", "Nút chính (4 trạng thái)", [("20", "kit_btn_primary_normal"), ("20", "kit_btn_primary_hover"), ("20", "kit_btn_primary_disabled")], "mọi màn", "240x64, 9-slice"),
 ("C02", "Nút", "Nút phụ (4 trạng thái)", [("20", "kit_btn_secondary_normal"), ("20", "kit_btn_secondary_hover")], "mọi màn", ""),
 ("C03", "Nút", "Nút phá hủy (4 trạng thái)", [("20", "kit_btn_danger_normal")], "A4 xác nhận xóa", "chỉ trong hộp xác nhận"),
 ("C04", "Nút", "Nút icon tròn và vuông", [("20", "kit_iconbtn_round_normal"), ("20", "kit_iconbtn_square_normal")], "A6, B1", "64 px, chưa có icon bên trong"),
 ("C05", "Điều hướng", "Tab (thường, hover, chọn)", [("20", "kit_tab_off"), ("20", "kit_tab_hover"), ("20", "kit_tab_on")], "A3b", ""),
 ("C06", "Ô", "Ô nhập (thường, focus, lỗi, thành công)", [("20", "kit_input_normal"), ("20", "kit_input_focus"), ("20", "kit_input_error")], "A3, A3b, A5", ""),
 ("C07", "Ô", "Dropdown (đóng, mở)", [("20", "kit_dropdown_closed")], "A5, B1", ""),
 ("C08", "Ô", "Toggle và checkbox", [("20", "kit_toggle_off"), ("20", "kit_toggle_on"), ("20", "kit_checkbox_on")], "B1", ""),
 ("C09", "Thanh", "Slider và knob", [("20", "kit_slider_track"), ("20", "kit_knob")], "A5, B1", ""),
 ("C10", "Thanh", "Thanh tiến trình", [("20", "kit_progress_track")], "A1, A3, A7, B8, B9", ""),
 ("C11", "Thanh", "Scrollbar", [("20", "kit_scroll_thumb")], "A4, B4", ""),
 ("C12", "Dòng", "Dòng danh sách (thường, xen kẽ, chọn)", [("20", "kit_row_normal"), ("20", "kit_row_alt"), ("20", "kit_row_selected")], "A4, A6, B3, B6", ""),
 ("C13", "Khung", "Panel S, M, L", [("20", "kit_panel_s"), ("20", "kit_panel_m")], "mọi màn", "viền 47 px"),
 ("C14", "Hộp thoại", "Popup có thanh tiêu đề", [("20", "kit_panel_popup")], "A3, A5, B1", "chưa có nút đóng, chưa có chân"),
 ("C15", "Nổi", "Toast và tooltip", [("20", "kit_toast"), ("20", "kit_tooltip")], "A2, A7, B3, B9", ""),
 ("C16", "Chỉ báo", "Tag trung tính và badge", [("20", "kit_tag_neutral"), ("20", "kit_badge")], "A4, A6, B9", ""),
 ("C17", "Chỉ báo", "Icon trạng thái (tích, X, cảnh báo, chấm)", [("20", "kit_icon_check"), ("20", "kit_icon_cross"), ("20", "kit_icon_warn"), ("20", "kit_icon_dot")], "mọi màn", ""),
 ("C18", "Khung", "Lớp tối nền modal", [("20", "kit_dim")], "mọi modal", ""),
 ("C19", "HUD", "Vitals, Quick bar, Minimap, Match, Killfeed, ô slot", [("15", "hud_slot_empty"), ("15", "hud_slot_selected"), ("15", "hud_panel_match")], "HUD, B9", "T-015 r11"),
]
# (ma, nhom, ten, ngu canh, man hinh, uu tien, ghi chu)
THIEU = [
 ("T01", "Nút", "Nút đóng (X) góc hộp thoại", "hộp thoại", "A3, A3b, A4, A5, B1, B8", "P0", "vuông xám đá + X, không đỏ; một vị trí duy nhất góc trên phải"),
 ("T02", "Hộp thoại", "Chân hộp thoại (đường chia + vùng nút)", "hộp thoại", "A3, A4, A5, B1, B8", "P0", "nút phụ trái, chính phải; hộp xác nhận cỡ nhỏ dựng từ cùng khung"),
 ("T03", "Điều hướng", "Header màn toàn phần (tiêu đề + quay lại)", "màn toàn phần", "A3b, A4, B4, B5", "P0", "kèm nút quay lại (mũi tên trái)"),
 ("T04", "Nút", "Bộ icon chức năng (khoảng 16)", "mọi ngữ cảnh", "A2, A4, B1, B3, B8", "P0", "cài đặt, bản đồ, túi, sao chép, làm mới, người, đồng hồ, wifi, âm thanh, nhạc, rung, mắt, sọ, mũi tên trái/phải, đóng"),
 ("T05", "Thẻ", "Thẻ phòng (tên, mã, 6/10, trạng thái, nút Vào)", "màn toàn phần", "A4", "P0", "thay dòng danh sách phẳng"),
 ("T06", "Thẻ", "Ô người chơi (rỗng, chưa sẵn sàng, sẵn sàng) kèm khung avatar", "màn toàn phần", "A6, B3, B6, B7", "P0", "PlayerSlot của Figma v2; avatar vẫn giữ chỗ (Q5)"),
 ("T07", "Ô", "Ô mã phòng 6 ký tự (rỗng, đang nhập, đầy, lỗi)", "hộp thoại", "A3", "P0", "đang mượn hud_slot của T-015"),
 ("T08", "Nút", "Keycap (phím) và nhãn phím", "mọi ngữ cảnh", "A7, B3, B5, B9", "P0", "đang mượn iconbtn vuông kéo dãn"),
 ("T09", "Nút", "Nút thứ ba (viền mảnh, nền trong) và nút chỉ chữ", "mọi ngữ cảnh", "A2, A4, B1", "P1", "nút chỉ chữ có thể không cần sprite, chỉ spec màu và gạch chân"),
 ("T10", "Thẻ", "Thẻ thống kê nhỏ (nhãn + số lớn)", "màn toàn phần", "B2, B6, B7", "P1", "panel S viền 47 px quá dày cho khối nhỏ"),
 ("T11", "Thẻ", "Thẻ nguyên nhân bị loại / kết quả (tiêu đề + dòng)", "màn toàn phần", "B2, B7", "P1", ""),
 ("T12", "Thẻ", "Ô vật phẩm lớn (túi đồ): thường, chọn, rỗng, đang kéo", "màn toàn phần", "B4", "P1", "icon vật phẩm chờ ART-04; GDD không có độ hiếm, loại vật phẩm = Weapon, Armor, Consumable"),
 ("T13", "Nổi", "Thẻ chi tiết vật phẩm khi rê chuột (biến thể lớn của tooltip)", "nổi", "B4", "P1", "tooltip nhỏ đã có"),
 ("T14", "Điều hướng", "Đường chia mục và tiêu đề mục", "hộp thoại, màn toàn phần", "B1, B4, B5", "P1", ""),
 ("T15", "Chỉ báo", "Phản hồi HUD: hit marker, cung sát thương, mũi tên nguy hiểm, chip hiệu ứng, vignette", "HUD", "B9", "P1", "đã gen thử ở T-021 (b/assets), chưa duyệt, chưa vào kit"),
 ("T16", "Chỉ báo", "Huy hiệu Thắng, Thua và marker bản đồ", "màn toàn phần", "B5, B6, B7", "P1", "đã gen thử ở T-021, chưa duyệt"),
 ("T17", "HUD", "Dải cảnh báo bom/bẫy, dòng nhặt đồ, nhắc phím F, thanh đang dùng đồ", "HUD", "B9", "P1", "đang dựng từ toast, row, progress: có thể chỉ cần spec, không sprite mới"),
 ("T18", "Khung", "Nền màn menu riêng và logo theo skin đá", "màn toàn phần", "A1 đến A7, B6, B7", "P1", "đang dùng gạch sàn của ảnh dựng thử HUD và Logo.png pixel kem (Q6)"),
 ("T19", "Chỉ báo", "Chỉ báo mạng (ping 3 mức)", "màn toàn phần, HUD", "A1, B8", "P2", ""),
 ("T20", "Nổi", "Menu chuột phải (context menu)", "nổi", "không màn nào trong GDD", "P2", "chưa cần; chỉ ghi để khỏi quên"),
 ("T21", "Thanh", "Spinner xoay (đang chờ không biết thời lượng)", "hộp thoại", "A3, B8", "P2", "đang dùng thanh tiến trình"),
 ("T22", "Thẻ", "Thẻ tài khoản có avatar", "màn toàn phần", "A2", "P2", "đang dùng toast"),
]
def counts():
    c = {"P0": 0, "P1": 0, "P2": 0}
    for t in THIEU: c[t[5]] += 1
    return c
def md_table():
    L = ["| Mã | Nhóm | Thành phần | Kit T-020 đã có | Thiếu | Ngữ cảnh | Màn T-021 cần | Ưu tiên |", "|---|---|---|---|---|---|---|---|"]
    for c in CO: L.append(f"| {c[0]} | {c[1]} | {c[2]} | có | {c[5] or '-'} | | {c[4]} | |")
    for t in THIEU: L.append(f"| {t[0]} | {t[1]} | {t[2]} | không | {t[6] or '-'} | {t[3]} | {t[4]} | {t[5]} |")
    return "\n".join(L)
def md_counts():
    c = counts()
    return "\n".join(["| | Số thành phần |", "|---|---:|", f"| Đã có trong kit T-020 và HUD T-015 | {len(CO)} |", f"| Thiếu P0 (chặn dựng đợt A và hộp thoại) | {c['P0']} |", f"| Thiếu P1 (đợt B và hoàn thiện) | {c['P1']} |", f"| Thiếu P2 (sau, hoặc chưa cần theo GDD) | {c['P2']} |", f"| Tổng thiếu | {sum(c.values())} |"])

def sprite(root, n, h):
    s = Image.open((T20 if root == "20" else T15) / f"{n}.png").convert("RGBA")
    if s.height > h or s.width > 330: k = min(h / s.height, 330 / s.width); s = s.resize((max(1, round(s.width * k)), max(1, round(s.height * k))), Image.NEAREST)
    return s
def draw():
    W = 2400; col = 3; cw = 760; gap = 30; x0 = 40
    nco = -(-len(CO) // col); nth = -(-len(THIEU) // col); ch = 210; th = 190
    H = 220 + nco * (ch + 20) + 120 + nth * (th + 20) + 60
    im = Image.new("RGBA", (W, H), (31, 29, 24, 255)); d = ImageDraw.Draw(im)
    d.text((40, 30), "T-026 UI kit: đã có và còn thiếu (skin A)", font=F(44), fill=(255, 220, 110))
    c = counts(); d.text((40, 100), f"Có {len(CO)} nhóm thành phần. Thiếu {sum(c.values())}: P0 {c['P0']} (chặn đợt A, hộp thoại), P1 {c['P1']} (đợt B), P2 {c['P2']} (sau).", font=F(26), fill=(237, 242, 238))
    d.text((40, 145), "Ô nét liền = đã có sprite. Ô nét đứt = thiếu. Nhãn P0, P1, P2 kèm chữ, không dựa màu. Chưa gen gì thêm, chờ Huy duyệt danh sách.", font=F(20), fill=(214, 218, 214))
    y0 = 200; d.text((40, y0 - 10), "ĐÃ CÓ", font=F(28), fill=(134, 179, 107))
    for i, (cd, grp, nm, sp, scr, nt) in enumerate(CO):
        x = x0 + (i % col) * (cw + gap); y = y0 + 40 + (i // col) * (ch + 20)
        d.rectangle([x, y, x + cw, y + ch], fill=(43, 40, 33, 255), outline=(150, 146, 120), width=3)
        d.text((x + 14, y + 10), f"{cd}  {nm}", font=F(22), fill=(237, 242, 238)); d.text((x + 14, y + 42), f"{grp} · {scr}" + (f" · {nt}" if nt else ""), font=F(17), fill=(214, 218, 214))
        xx = x + 20
        ss = [sprite(r, n, 100) for r, n in sp]; tot = sum(q.width for q in ss) + 18 * (len(ss) - 1); k = min(1, (cw - 40) / tot)
        for q in ss:
            if k < 1: q = q.resize((max(1, round(q.width * k)), max(1, round(q.height * k))), Image.NEAREST)
            im.alpha_composite(q, (xx, y + 80 + (100 - q.height) // 2)); xx += q.width + 18
    y1 = y0 + 40 + nco * (ch + 20) + 20; d.text((40, y1), "THIẾU", font=F(28), fill=(255, 138, 120))
    for i, (cd, grp, nm, ctx, scr, pr, nt) in enumerate(THIEU):
        x = x0 + (i % col) * (cw + gap); y = y1 + 50 + (i // col) * (th + 20)
        for k in range(x, x + cw, 22): d.line([(k, y), (k + 12, y)], fill=(200, 200, 190), width=3); d.line([(k, y + th), (k + 12, y + th)], fill=(200, 200, 190), width=3)
        for k in range(y, y + th, 22): d.line([(x, k), (x, k + 12)], fill=(200, 200, 190), width=3); d.line([(x + cw, k), (x + cw, k + 12)], fill=(200, 200, 190), width=3)
        d.rectangle([x + 4, y + 4, x + cw - 4, y + th - 4], fill=(36, 34, 29, 255))
        filled = pr == "P0"
        d.rounded_rectangle([x + 14, y + 14, x + 84, y + 50], radius=6, fill=(255, 220, 110) if filled else None, outline=(255, 220, 110) if pr != "P2" else (170, 170, 160), width=3)
        d.text((x + 49, y + 32), pr, font=F(22), fill=(31, 29, 24) if filled else ((255, 220, 110) if pr == "P1" else (170, 170, 160)), anchor="mm")
        ttl = f"{cd}  {nm}"; w1 = ttl
        if d.textlength(ttl, font=F(21)) > cw - 115:
            parts = ttl.split(); w1 = ""
            for w in parts:
                if d.textlength(w1 + " " + w, font=F(21)) > cw - 115: break
                w1 = (w1 + " " + w).strip()
        d.text((x + 100, y + 16), w1, font=F(21), fill=(237, 242, 238))
        if w1 != ttl: d.text((x + 100, y + 42), ttl[len(w1):].strip(), font=F(21), fill=(237, 242, 238))
        d.text((x + 14, y + 74), f"{grp} · {ctx}", font=F(17), fill=(214, 218, 214)); d.text((x + 14, y + 100), f"Màn: {scr}", font=F(17), fill=(214, 218, 214))
        words = nt.split(); line = ""; yy = y + 128
        for w in words:
            if d.textlength(line + " " + w, font=F(16)) > cw - 30: d.text((x + 14, yy), line, font=F(16), fill=(190, 192, 186)); yy += 22; line = w
            else: line = (line + " " + w).strip()
        if line: d.text((x + 14, yy), line, font=F(16), fill=(190, 192, 186))
    im.convert("RGB").save(OUT / "thieu-gi.png"); return im.size
if __name__ == "__main__":
    OUT.mkdir(exist_ok=True)
    (OUT / "bang-doi-chieu.md").write_text(md_table() + "\n\n" + md_counts(), encoding="utf-8")
    print(draw(), counts(), len(CO))
