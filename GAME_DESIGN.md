# Pup Launch — Tài liệu thiết kế game (GDD)

> Bản 10 · 12/11/2026 · **đã chốt, đang thực hiện** (người dùng: "từ báo cáo đó thiết kế lại game, redesign lại toàn bộ cx
> đc, vẽ thì dùng chatgpt nhé phải thật mượt mà" → "làm đi"). Nguồn: `docs/cpc-reference-report.md` (chơi thật 3 lần + 5 video).
> Bản 9 (11/11) vẫn đúng ở mọi chỗ §R không ghi đè.
> Tên tiếng Việt tạm: **Hải Cẩu Bay** · Tên mã trong code: `PufferLaunch` · Bản cũ: `docs/archive/GAME_DESIGN_v6_2026-11-11.md`

Hải cẩu con Pip và bầy bạn giành lại kho cá mùa đông từ bầy gấu bắc cực. Mỗi màn hai pha: **đập đúng nhịp** để hải cẩu
đang đu dây bay lọt khe thoát của hố băng, rồi **chạm đúng lúc** để từng con lao chéo xuống đấu trường hạ hết gấu.
Bố cục màn chơi học theo thể loại "máy bắn + phá công trình" (ảnh mẫu Crazy Penguin Catapult bản Android); nhân vật,
hình, âm thanh, chữ và màn chơi là của riêng game.
Báo cáo chơi thật CPC (12/11/2026, 3 lần chơi + 5 video + đối chiếu báo cáo repo FrostFling): `docs/cpc-reference-report.md`.

Ký hiệu trạng thái trong tài liệu: ✅ đã có trong build · 🔧 có nhưng cần làm lại/hoàn thiện · 🆕 chưa làm · ❓ cần bạn chốt.

## R. Bản 10: làm lại toàn bộ theo báo cáo (12/11/2026)

Mục này **ghi đè** các mục sau khi khác nhau. Mọi thứ vẫn là của riêng game: hải cẩu, gấu ác, Vịnh Băng Giá, chữ, màn chơi.
Chỉ học **nhịp, bố cục, cách phản hồi** của thể loại.

### R1. Hình vẽ: toàn bộ bằng ChatGPT
- **Mọi thứ người chơi nhìn thấy** đều do ChatGPT vẽ: nhân vật, nền hố bắn, nền đấu trường, ô đá/băng/gỗ, máy bắn, bản đồ,
  menu, khung UI, nút, icon, tranh truyện mở đầu, tranh thắng/thua. `make_art.py` (SVG) chỉ còn làm hình dự phòng khi
  chưa có tranh, và vẽ những thứ thuần hình học (vệt, hạt).
- Cách làm: Claude tự lái ChatGPT trong Chrome. Luôn đính kèm một tranh đã chốt và bấm **Chỉnh sửa** để giữ nét (tranh mới
  từ đầu bị lệch nét). Tranh tải về chuyển vào `Assets/Art/Reference/`, script trong `Tools/` cắt ra `Assets/Resources/Art/`.
- Ô lặp (đá cuội, băng, gỗ) phải lặp không thấy mối nối. ChatGPT không tự làm được, nên `Tools/` có bước làm liền mép.
- Nền hố bắn và bản đồ là **một tranh lớn** vì giống nhau mọi màn. Va chạm đặt theo tranh, toạ độ ghi trong code.
  Đấu trường đổi theo màn nên vẫn ghép từ ô và vật thể.
- Phong cách chung: cartoon 2D vẽ tay, viền nâu-navy dày, tô màu mềm hai tông, highlight trắng. Hố bắn lạnh (xanh băng, đá
  xám, mây hồng), đấu trường ấm (hoàng hôn cam, băng xanh trong).

### R2. Mượt mà
- Chạy **60 fps** (`Application.targetFrameRate = 60`), không cấp phát mỗi frame.
- Vòng lặp chính có **8 frame vẽ tay**: bay, đu dây, đi, gấu thở. Hành động ngắn có 4–6 frame.
- Thêm chuyển động phụ bằng code cho mọi nhân vật:
  - nén-giãn (squash/stretch) khi đáp, va, bật lên;
  - lắc nhẹ khi đứng (thở);
  - ease cho mọi tween;
  - camera bám mềm (SmoothDamp), rung theo lực va.
- UI: panel bật vào có nảy (overshoot), nút nhún khi chạm, số đếm chạy lên, sao bay vào từng cái.
- Chuyển cảnh không bao giờ cắt cụt: nước dâng/rút, panel trượt.

### R3. Pha 1 (thay §4 khi khác)
- **Máy bắn đặt bên phải dưới đáy cung đu.** Sửa 12/11 sau khi xem video Flash từng 0,06 s: cầu tuyết **gắn cố định ở
  đầu cần, không bay đi**. Chạm thì cần quét lên qua đỉnh, chính quả cầu **đập vào hải cẩu** (có chớp sáng chỗ va).
  Cần chỉ với tới đoạn cung từ đáy sang nửa phải, nên phải chạm lúc hải cẩu đi qua đó. Trúng thì hải cẩu văng
  lên-phải; quét hụt thì cần quay về, nạp lại.
- **Đu rộng**: biên độ 46°–57° (Flash gần nằm ngang ở hai đầu), chu kỳ khoảng 1,1 s.
- Ba mức phản hồi (chữ to giữa màn, nảy vào rồi mờ, luôn kèm hình chứ không chỉ màu):
  - **"TUYỆT!"** vàng có sao: trúng trong cửa sổ vàng (±8° quanh đáy). Hải cẩu bay thẳng vào khe, xoay vòng đẹp.
  - **"OK!"** cam: thoát được.
  - Con thoát **bay luôn ra khỏi màn, không đứng chờ trên gờ** (user 12/11: "hải cẩu bay luôn chứ ko đứng nữa"). Vận tốc
    lúc lọt khe được ghi lại (`LaunchPhase.Exits`).
- **Khe khó hơn** (user 12/11: "bay qua khe khá dễ"): `aimAssist` 0.65 → 0.4, chỉ cú "TUYỆT!" mới được nắn thẳng vào khe.
  Lần 2 ("khe to còn quá dễ qua"): tường phải cao thêm 1.0 (`WallRaise`; thử 1.5 thì chạm mù chỉ lọt 1/6 trên máy ảo, quá khó), vẽ bằng một lớp đá cuội cùng kiểu đấu
  trường (`rock_cobble` + viền tuyết `rock_top`). Mô phỏng quỹ đạo (13 đơn vị/s, trọng lực 0.55, cản 0.1) từ đáy cung:
  khe gốc nhận góc phóng 32°–52°, tường cao 1.0 còn 40°–52° (1.5 là 44°–52°); kéo dài trần sang phải thì chặn luôn cú hoàn hảo nên
  không làm. `strikeAngle` 52 → 48 để cú "TUYỆT!" nằm giữa cửa sổ. Bot trong test chạm lúc hải cẩu đu về đáy
  (`LaunchPhase.ApproachingBottom`).
  - **"ỐI!"** đỏ: văng lệch. Hải cẩu **kẹt lại đến hết pha, giãy chân** (frame `stuck`): đập trần thì cắm ngược trong nhũ
    băng; đập tường thì trượt xuống nằm ngửa trên sàn hố (không đè lên máy bắn). Số hải cẩu kẹt là thước đo thất bại
    nhìn thấy được. Thay cho rơi mất khỏi màn. *Quyết của Claude: nóc trần nằm ngoài khung nhìn nên kẹt "trong nhũ băng".*
- **Bắn hụt:** không mất hải cẩu, chỉ mất thời gian nạp lại máy. (Bỏ "cầu tuyết bay trúng con đang chờ": cầu không bay.)
- **Con kế tiếp** đi tới đầu cầu, cúi, nắm dây, tụt xuống (hoạt ảnh nắm dây mới). Biên độ đu tăng dần theo từng con.
  Tốc độ đu tăng theo màn.
- Hết hải cẩu: chuyển cảnh nước dâng như cũ.

### R4. Pha 2 (thay §5 khi khác)
- **Thẻ nhiệm vụ** trước pha 2: "Màn N · Hạ X gấu" + "Tổng gấu: T" + hình hải cẩu **khuỵu xuống rồi đứng lên chào** (CPC: cúi
  nhặt mũ rồi chào, §14.3; dùng frame leap_0 + salute có sẵn; frame grab có vẽ dây nên không dùng). Nút Tiếp tục.
- Hải cẩu bay ngang, ra mép phải thì vào lại mép trái, **bay mãi tới khi chạm** (bỏ `maxLoops`). Sau 2 vòng, icon ngón tay
  nhấp nháy nhắc chạm.
- **Hải cẩu pha 2 nhỏ hơn**: ×0.75 so với pha 1 (`PufferController.ArenaScale`, collider co theo).
- **Lướt giữ nguyên tốc độ** (không cản khi lướt); cản `airDrag` chỉ bật khi lao hoặc va. Trước đây lướt chậm dần nên con
  vào chậm không bao giờ bay hết 2 vòng: không có gợi ý chạm, không tự lao, màn không kết thúc được (review 12/11).
- **Lao cắm đầu, nhanh** (xem lại CPC từng khung 12/11 đêm, báo cáo §14.2, thay cho bản "lao chéo chậm" trước đó): chạm
  thì hải cẩu cắm đầu và lao theo hướng 65° dưới ngang (`diveAngle`) với tốc độ bằng 2 × tốc độ bay (`diveSpeedScale`),
  `diveGravity` 0.4 kéo dốc dần. Chạm **ngay trước** con gấu (dẫn trước khoảng nửa độ cao).
- **Quỹ đạo pha 1 quyết định đường bay pha 2** (`AttackPhase.Entry`): con thoát khe với góc dốc (80°) vào cao 7.8 trên
  sàn và bay chậm; góc thoải (25°) vào thấp 6.0 và bay nhanh. Tốc độ ngang = 0.55 × tốc độ ngang lúc thoát, kẹp 3.2–6.2.
  Mỗi con một đường bay, nên phải canh lại thời điểm lao. Con không có dữ liệu thoát dùng mặc định (cao 7.0, 4.5).
- **Chạm ở đâu cũng nhận** (bản tham chiếu có lúc nuốt chạm: không làm theo).
- Gấu đứng yên tại chỗ, mỗi con một kiểu đứng chơi lặp lại:
  - đấm gió;
  - ăn tô cá;
  - ngồi ôm gối, xoay đầu;
  - khoanh tay vênh mặt;
  - hai con huých nhau;
  - ngủ;
  - tung hứng;
  - cười.
  Thêm bộ **punch / sit / smug / nudge**.
- Nảy chuỗi + combo "×2/×3" (đã có). Băng vỡ thành mảnh vuông nhỏ, khối trên mất đỡ thì sập theo. Gấu trên khối bị phá
  rơi theo và vẫn tính. Hải cẩu sau va chạm: lăn, bụi tròn, ngồi choáng, đứng dậy đi ra.
- HUD: trên-trái số hải cẩu; giữa 4 nút tròn (loa, nhạc, ?, tạm dừng); trên-phải mặt gấu + "Tổng T" + "đã hạ/cần".
- Màn chỉ kết thúc khi con cuối dừng hẳn.

### R5. Kết quả, thua, thống kê
- **Thắng:** panel "Màn N" + "Thắng rồi!" (đủ chỉ tiêu) hoặc **"Hoàn hảo!"** (hạ hết gấu). Tranh: bầy hải cẩu nhảy múa quanh
  gấu nằm ngửa mắt xoáy. Dòng "Gấu đã hạ X/Y", "Tổng gấu T", sao 1–3 bay vào, nút Bản đồ · Chơi lại · Tiếp.
- **Thua:** panel **"Thất bại!"** + mặt gấu + số đã hạ + 3 sao rỗng. Nút theo R9 (Bản đồ · Chơi lại · Quảng cáo: +1 hải cẩu · Tấn công lại); Thống kê không nằm trên thẻ thua. Sau khi
  đóng panel: cảnh ngắn gấu xách lồng có hải cẩu chạy ngang (đã có ở màn tải).
- **Thống kê** (panel, mở từ Cài đặt): tổng gấu đã hạ, số lần ×2, ×3, ×4 trở lên, màn Hoàn hảo. Lưu trong
  save, kiểm tra từng trường.
- Màn 1–3 là màn dạy: rộng tay, gần như không thua. Từ màn 4 siết lại để thua là có thể xảy ra.

### R6. Truyện mở đầu
- Khoảng 20 s, có nút **Bỏ qua**. Hiện ở lần chạy đầu, xem lại được từ menu.
- 5 tranh ChatGPT, mỗi tranh có lia/zoom chậm + vài sprite động:
  1. Làng hải cẩu ngày nắng, bầy hải cẩu chơi trên băng.
  2. Bầy gấu ác đứng khoanh tay trên đồi, cười gian.
  3. Gấu lăn một quả cầu tuyết khổng lồ, cuốn bầy hải cẩu con vào.
  4. Gấu nhốt hải cẩu vào lồng, xách đi. Kho cá bị khiêng theo.
  5. Pip trốn thoát, đội mũ len, nhìn về phía máy bắn của ông Hải Mã.
- Mỗi cảnh có một dòng chữ (qua `Loc`).

### R7. Bản đồ và menu
- Vẽ lại bằng ChatGPT: đảo băng nhìn từ trên, đường đá tròn uốn chữ S. Lồng có hải cẩu kêu "CỨU!" ở nút boss. Nút đã
  qua có sao, nút hiện tại có Pip đứng nhún.
- Menu chính: tranh tiêu đề ChatGPT (logo "Pup Launch" + cảnh hoàng hôn), nút Chơi to, nút phụ nhỏ.

### R7b. Bố cục hố bắn theo tranh (đã làm 12/11)
Toạ độ đo trên `pit_bg.png` (1536×1024), tranh cao 11 đơn vị, tâm (0, 0.5): tường trái x −7.0, tường phải x 6.0 cao tới
y 2.3 (gờ chờ), trần dưới y 4.5, đầu nhũ y 3.8, mép trần x 2.4, sàn y −3.3, mặt cầu y 1.7 tới x −1.3, neo dây (−1.5, 1.6).
Dây dài 1.9 (cũ 2.4) để đáy cung nằm ngay trên cốc máy bắn đặt trên sàn tranh. Va chạm là hộp vô hình (`LaunchPhase.Solid`).
Đổi tranh thì đo lại các số `W(px)`/`H(px)` trong `LaunchPhase.cs`.

### R7c. Bổ sung từ báo cáo FrostFling (đã làm 12/11)
- **Pha 1 kiểu bungee** (`LevelData.launcher`): màn 5 và 8. Dây treo thẳng đứng co giãn, hải cẩu nhún lên xuống trên
  máy bắn. Đáy nhún trùng điểm máy bắn ngắm (đáy cung của dây đu). Biên độ 0,4–0,75 và tốc độ tăng theo từng con.
  "TUYỆT!" khi dây giãn hết. Ray ngắm 40° (`strikeAngle` − 8), kéo 0,7 (`BungeeAssist`; 0,85 cho chạm mù lọt 13/14 ở QA
  12/11 màn 5 và 8), nên đập lúc bầu dây đang rơi xuống mới lọt khe.
- **Sao bay vào bộ đếm gấu** mỗi khi hạ một gấu.
- **Lồng nổ tung** ở màn boss: chấn song bay ra, sao xoay, hải cẩu được thả đáp đất vẫy tay.
- **Ăn mừng trên bụng gấu**: hải cẩu dừng trên gấu đã gục thì đứng lên ăn mừng.
- **Ninja đá chéo** vào gấu đứng gần nhất phía dưới (thay cho rơi thẳng khi có gấu trong tầm 5 đơn vị).
- **Vua Gấu bốc hơi giận** từ hai tai khi đứng trên bệ.

### R7d. Sau khi chơi lại bản flashmuseum (đã làm 12/11)
- **Hang ngầm** (màn 6, 9): khe trên mặt đất rơi xuống một hang đá tối có nhũ băng. Hang sâu `AttackPhase.CaveDepth`
  2,6, trần hang dày 0,8, camera xuống theo thêm 1,6. Màn 9: nút mở cổng băng nằm dưới hang.
  Khe phải rộng ≥ 1,5 vì hải cẩu cuộn tròn có đường kính khoảng 1.
- **Gấu bị hạ văng lên xoay 1 vòng** (0,55 s) kèm mảnh băng, rồi nằm lại.
- **Choáng = 3 quả cầu tuyết nhỏ xoay quanh đầu**, cả ở pha 2 (đứng dậy) lẫn pha 1 (kẹt).
- **Vệt gió trắng** khi bổ nhào nhanh.
- **Gấu ngồi thõng chân** ở mép bệ (`BearMood.Dangle`), đặt sát mép, chân treo ngoài.
- **Thẻ nhiệm vụ**: hải cẩu chào tay (4 frame lặp).

### R7e. Giao diện theo bản tham chiếu (người dùng 12/11 đêm: "thiết kế lại UIUX, đồ họa"; báo cáo §15)
- **HUD** (cả hai pha): trên-trái mặt hải cẩu tròn (`hud_pup`) + "× N" chữ to viền; trên-giữa 4 nút tròn (loa, nhạc, ?,
  tạm dừng); trên-phải (chỉ pha 2, hiện khi vào đấu trường) mặt gấu tròn (`hud_bear`) + "Tổng N" nhỏ trên + "đã hạ/cần"
  to dưới. **Không có điểm và thanh sao trên HUD**: điểm chỉ hiện ở bảng kết quả. Quy ước góc ở §10 thay bằng dòng này.
- **Thẻ nhiệm vụ** đứng chờ nút **Tiếp tục** (chạm bất kỳ cũng được; bot test gọi `GameManager.ContinueMission`): "Nhiệm
  vụ N", hải cẩu khuỵu rồi chào, "Hạ N gấu để sang màn tiếp theo", "Tổng gấu: N" (tổng mọi lần chơi), "N hải cẩu thoát".
- **Kết quả**: "Nhiệm vụ N" / "Chúc mừng!" (hoặc "Thất bại!"), hải cẩu đứng trên bụng gấu, "Gấu đã hạ: d/r", "Tổng gấu:
  N", rồi phần riêng của game: 3 sao + điểm, nút Bản đồ / Chơi lại / **Tiếp tục** (vàng, sang màn kế).
- **Tạm dừng**: màn tối đi, chữ "TẠM DỪNG" lớn, hàng nút Tiếp tục / Chơi lại / Bản đồ / Âm thanh (không có panel).
- **Bản đồ**: nút màn là **đá "?"** (`node_stone`), màn đã qua = **đá có sao vàng** (`node_star`), màn kế = hải cẩu đứng
  trên **đệm xanh phát sáng** (`node_glow`); boss = lồng trên đá, màn thưởng = cá; không số màn, không chú thích, không tên
  vùng. Giữ của game: đếm sao góc phải, nút Nâng cấp, hàng 3 sao nhỏ dưới đá đã qua, khóa dưới đá chưa mở.
- **Menu chính**: logo ngang giữa trên, nút Chơi vàng to ở giữa, hàng Cách chơi / Cài đặt / Giới thiệu dưới; loa / nhạc /
  ? trên-giữa. **Lần Chơi đầu** (chưa có sao màn 1) vào thẳng màn 1, không qua thẻ Cách chơi và bản đồ: màn 1 tự dạy bằng gợi ý trong game (R9, 15/11; trước đó mở thẻ Cách chơi).
- **Tường phải hố bắn** cao thêm `WallRaise` ngay trong tranh: `Tools/prep_backgrounds.py` `raise_wall()` kéo giãn phần
  trên của tường vẽ lên `WALL_RAISE` 98 px (= 1,0 đơn vị; đổi `WallRaise` thì đổi cả hai). Không vẽ khối đá riêng đè lên:
  đá nâu của đấu trường lệch màu với đá xám của hố (người dùng 13/11: "tường ko nhất quán").
- Sau khi lao, camera **giữ ở mức thấp nhất đã xuống** cho tới khi con đó xong (CPC §16.2); nảy khỏi gấu `bearBounce` 7,5.
- Màn dạy chơi (`showTutorial`) kéo cú bắn về khe mạnh hơn: `TutorialAssist` 0,8 thay cho `aimAssist` 0,4.
- **Boss theo CPC** (người dùng 13/11: "cpc đâu có thanh máu", "cx ko có vua gấu"; báo cáo §10.6): boss là **gấu cỡ
  thường** bốc hơi giận trên bệ, **không thanh máu, không "Vua Gấu" khổng lồ**. Hải cẩu lao trúng trực tiếp = mất 1 mạng
  (sao, choáng); màn 12 boss có 2 mạng. Nút sàn chỉ **hất boss khỏi bệ xuống sàn một lần** (dễ trúng hơn), không trừ mạng.
  Các mục cũ nói "Vua Gấu Băng to gấp đôi / thanh máu dọc / trèo lại bệ" ở §0, §2, §8, §11 không còn đúng.
  `ui_bar` (64×32 px) dùng viền 9-slice 14 px (viền 44 px chung của `ui_*` lớn hơn ảnh).
- **Giữ cửa hàng và sao** (người dùng 13/11: "vẫn giữ cửa hàng"): bản đồ có đếm sao + nút Nâng cấp + hàng sao nhỏ; bảng
  kết quả có 3 sao. **Bỏ điểm**: không hiện điểm ở kết quả, không còn chữ "+điểm" bay lên (điểm vẫn tính ngầm cho sao thứ 3).
- **Pha 1 theo CPC §16.1** (người dùng 13/11: "xem kĩ lại cpc để sửa lại pha 1 và 2"): lối thoát là cả khoảng hở trên tường
  phải (`WallRaise` 0, tranh không kéo tường nữa: `WALL_RAISE` 0), cú bắn dẹt hơn (`strikeAngle` 40). Thay các số 12/11
  (tường nâng 1,0, góc 48).
- **Pha 2 theo CPC §16.2**: mọi con bay ngang cùng độ cao và tốc độ (`Entry(null)`: cao 7,0, 4,5 u/s); **bỏ liên kết cú bắn
  pha 1 → đường bay pha 2** (`GameManager` không truyền `launch.Exits` nữa).
- **Khối đứng yên** (CPC §17.1): mọi `Block` là `Static`; khối bị trúng thì vỡ, khối bên cạnh giữ nguyên chỗ, không có tháp đổ.
  Gấu vẫn là vật lý nên rơi khi khối dưới chân vỡ.
- **Vật lý và hoạt ảnh theo CPC** (người dùng 13/11): mọi hải cẩu nảy thêm `BaseBounces` 2 lần như quả bóng sau cú trúng đầu
  (nâng cấp Cuộn tròn cộng thêm), kéo **đuôi khói xám** (`Fx.Smoke`) khi đang nảy; **gấu luôn quay mặt sang trái** (phía
  hải cẩu bay vào; người dùng 13/11), không quay theo hải cẩu.
  Màn 2 = cổng vòm (2 cột 3 khối + dầm 5 khối, cần 3), màn 3 = tháp 6 khối + dầm chữ T + bậc băng (cần 4), mỗi màn 6 con;
  `Design.Row` xếp dầm ngang.
- **Mũ văng** (`pup_hat`) ở cú trúng đầu tiên; **vết lõm** (`dent`) chỗ trúng đá; **khối nêm băng** (`block_wedge`,
  `BlockSpec.wedge` ±1, `Design.Wedge`) ở chân cột và đầu dầm; **màn nước kéo từ trên xuống** giữa hai pha
  (`HudView.Curtain`): phủ hố, camera sang đấu trường sau màn nước, rồi kéo lên lộ thẻ nhiệm vụ. Hình mới:
  `Assets/Art/Reference/arena_extras.png`. Sau khi mũ văng, quả bóng đổi sang `pup_ball_bare` (đầu trần). Bộ hình đầu trần (đứng, vùng vẫy, đi, choáng,
  đứng dậy, cổ vũ, bóng) cắt từ `pup_sheet_{a,c,e}_bare.png` và `pup_walk_bare.png` (ChatGPT sửa bảng gốc, bỏ mũ) bằng
  `slice_sheets.py` với cùng tỉ lệ bảng gốc; `PupRig.GoBare()` chuyển sang bộ này. `pup_ball_bare.png` trong Reference là
  bản thử đầu, không còn dùng. Shortcut: khi
  đứng dậy đi ra thì hình vẫn có mũ. Màn 5, 6, 7, 9, 10 đã thêm cổng vòm, dầm che gấu và nêm theo kiểu CPC.
- **Bỏ nốt những thứ CPC không có** (người dùng 13/11): thẻ "Màn N" đầu màn, tuyết rơi nền. Bản đồ có chữ **"CỨU!"** đỏ
  nháy cạnh lồng cho tới khi thắng boss (`help_cry`).
- **Va chạm theo CPC, đo lại từng khung 13/11 (báo cáo §18)**: gấu trúng đòn lộn nhào có sao, nhảy nhẹ (`KnockUp` 3,5) rồi
  **rơi xuyên sàn, biến khỏi màn** (không nằm lại; bỏ cổ vũ trên bụng gấu). Hải cẩu nảy thấp: `bearBounce` 3,6,
  `bounceKeep` 0,5, vật liệu nảy 0,15; **trượt** trên tuyết và dốc (ma sát 0,08).
- **Pha 1 giữ khó + liên kết sang pha 2** (người dùng 13/11 tối: "còn quá dễ", "chưa có nhiều quỹ đạo ảnh hưởng pha 2"):
  `WallRaise` 1,0, `strikeAngle` 48, `TutorialAssist` 0,6, `GameManager` lại truyền `launch.Exits`. Bỏ thử nghiệm "lối
  thoát rộng, bay ngang cố định" của buổi chiều.
- **Hố bắn là hộp đá kín như CPC** (người dùng 13/11: "bố cục đất đá chưa giống cpc ở pha 1"): trần đá chạy suốt, đầu phải
  cắt chéo; cột tường phải cao (đỉnh `RightWallTop` = H(320) (ở 350 chạm mù lọt 6/6 trên máy ảo); ở 250 không con nào lọt, ở 290 chỉ cú 41° hoàn hảo lọt, 35° và 47° trượt). Tính tay: khe nhận góc khoảng 37–44° từ đáy
  cung đu (mái chéo chặn phía trên), nên `strikeAngle` 41 và ray bungee không hạ góc nữa (`BungeeAimDrop` 0); lối thoát duy nhất là khe chéo giữa hai thứ đó. Sàn: khối
  đá dưới cầu, dốc xuống **hốc trũng** đặt máy bắn, bờ thấp dốc lên chân cột. Tranh: `pit_bg_box.png` (bố cục) + cột của
  `pit_bg_box2.png` hạ 220 px + viền `pit_side_walls_box.png`, ghép bởi `Tools/prep_backgrounds.py`. Va chạm đo lại trên
  tranh mới (`Poly` cho mái chéo, khối trái, bờ phải). `WallRaise`/`raise_wall` không còn dùng (`WALL_RAISE` 0).
- **Đá pha 2 = đá pha 1** (người dùng 13/11: "pha 2 đất đá chưa nhất quán"): `rock_cobble` và `rock_ramp` giờ cắt từ chính
  tranh đá của hố (`pit_side_walls_box.png`, nửa trái) trong `prep_backgrounds.build_terrain`; không còn dùng đá nâu và
  dốc vẽ riêng của `arena_terrain.png` (chỉ còn lấy viền tuyết `rock_top` từ đó).
- **Dốc đá pha 2** vẽ bằng chính ô đá cuội lát của khối (`rock_cobble`) trên một **lưới tam giác** cùng tỉ lệ ô đá (SpriteMask không hiện
  khi chụp; `rock_ramp` không còn dùng), viền tuyết `rock_top` xoay theo mặt dốc: dốc và khối cùng cỡ đá, cùng viền.
- **Hải cẩu bắn trượt** (pha 1): **đập vào đâu dính ngay tại đó** đến hết pha (người dùng 13/11), đầu cắm vào đá, chân
  đạp ra ngoài theo pháp tuyến mặt đá (trần, mái chéo, tường, bờ, sàn). Không rơi, không dời chỗ.
- Mọi mảnh đá pha 2 (gò, sàn, vách, dốc) vẽ bằng `AttackPhase.Cobble`: lưới phủ vân đá theo **toạ độ thế giới**, nên đá
  chạy liền từ mảnh này sang mảnh kia, không còn đường nối. Hải cẩu trượt ở pha 1 dùng tư thế `Stuck` xoay theo mặt đá.
- Ô đá `rock_cobble` làm lặp bằng `crossfade_tile` (hoà chồng mép 100 px); `seamless()` cũ để lại đường cắt cứng giữa mỗi ô.
- **Viền tuyết** `rock_top` và **khớp tròn** `snow_knob` do `prep_backgrounds.draw_snow` vẽ (trắng, bóng xanh nhạt, nét
  navy như tuyết của hố); không lấy từ `arena_terrain.png` nữa (dính dải đá nâu). Mỗi đầu dốc có một khớp tròn đặt đúng **giao điểm hai đường tâm
  dải tuyết** (dốc với mặt bằng trên/dưới); dải viền dốc chạy đúng từ khớp này tới khớp kia. Khối không có khớp riêng.
- **Tuyết và đá pha 2 lấy thẳng từ tranh hố pha 1**: `rock_top` là dải mũ tuyết vẽ tay của hố (cắt từ
  `pit_side_walls_box.png`), cao `RimHeight` 0,7; ô đá `rock_cobble` trải 5,5 đơn vị mỗi ô để viên đá to cỡ đá của hố.
- **Tranh hố là MỘT tranh liền** `pit_bg_wide3.png` (16:9, 1672×941; đá bao quanh nằm sẵn trong tranh), `PaintingHeight`
  12,64 nên rộng 22,5 đơn vị, đủ cho 20:9 (21:9 hở 0,4 đơn vị mỗi bên). **Mặt trên cột phải dốc chéo song song với mái
  chéo** (người dùng 13/11), thành máng thoát chéo; va chạm `Column Slope`. Không còn ghép cột, ghép viền (`pit_bg_box*.png`, `pit_side_walls_box.png` chỉ
  còn là nguồn vân đá/tuyết cho pha 2). Số đo `LaunchPhase` lấy lại trên tranh này (mép trong cột H(355)).
- **Đá pha 2** phủ bằng một mảng tường đá nguyên của tranh (không hoà chồng), lật gương theo chiều ngang
  (`wrapModeU = Mirror`), cao 11 đơn vị nên không lặp theo chiều dọc.
- Vùng "đập đẹp" của dây đu `SweetWindow` 4° (8° cho 6/16 nhịp lọt máng, quá dễ).
- **Pha 2 xa hơn, bay cao hơn, lao chéo chậm hơn** (người dùng 13/11): `AttackZoom` 1,15 (trước 0,9), tâm khung nhìn cao
  4,5 trên sàn, hải cẩu vào ở độ cao 7,4–9,0 (`SpawnPoint` 8,2). Lao `diveAngle` 45° (trước 65°), `diveSpeedScale` 1,3
  (trước 2). Hải cẩu bay vào và vòng lại **ngoài mép khung nhìn** (`AttackPhase.ViewLeft/ViewRight`), không hiện ra giữa màn.
  Bầu trời rộng 30, sàn kéo thêm 12 về bên phải cho khung nhìn rộng.
- **Tuyết pha 2 là MỘT dải liền** (người dùng 13/11: "tuyết nối vẫn chưa khớp"): `AttackPhase.BuildSnow` lấy đường
  viền cao nhất của mọi mặt đá (mặt khối, mặt dốc), phủ một dải lưới `rock_top` chạy theo nó, gấp góc vát (miter) ở mỗi
  khúc cua, vân tuyết liền mạch; chỉ ngắt ở vách đứng. Khối và dốc không còn dải tuyết riêng, không còn khớp tròn.
- Hải cẩu bay cao hơn nữa: vào ở 8,8–10,4 (`SpawnPoint` 9,6), `AttackZoom` 1,3, tâm khung nhìn 5,3 trên sàn.
- Gờ băng nổi dùng cùng dải tuyết `SnowStrip`. Vết lõm chỉ in trên vách đá đứng (mặt trên và dốc đều có tuyết phủ). Mũ văng là vật lý
  nhỏ (không va gấu, không va hải cẩu), nằm trên đá 2,5 giây rồi mờ dần (`FadeAway`).
- **Gờ nổi bên phải là đá cuội** (người dùng 13/11: băng không vỡ được thì thay bằng đá): chỉ khối phá được mới là băng.
- **Độ bền khối theo CPC** (người dùng 13/11: "có các loại băng sau bao nhiêu lần chạm mới vỡ, xem lại cpc"; báo cáo §19):
  CPC chỉ có **một loại băng, vỡ ngay lần chạm đầu**; **thùng gỗ không vỡ khi bị đập**, chỉ biến mất khi đáp nút cùng nhóm
  (cầu thùng gỗ ở màn boss). Game: băng và tuyết vỡ khi hải cẩu chạm (bỏ máu theo lực va); `Stone` (vẽ là thùng gỗ) bất
  hoại trừ khi là cổng có nhóm. `iceHp`/`snowHp`/`stoneHp`/`minDamageImpulse` không còn dùng cho va chạm.
- **Va chạm theo CPC màn 1–5 chơi tay** (báo cáo §20): hải cẩu **đang bay ngang chạm băng thì phá khối và bay xuyên tiếp**;
  **lao trúng băng** thì khối vỡ, hải cẩu bật ngược sang trái ngắn (`IceRebound` 1,4); **lao trúng dốc đá thì không nảy mà
  trượt bụng xuống dốc** (tối thiểu `SlideSpeed` 4) với tia lửa ma sát; nảy khỏi gấu thấp (`bearBounce` 2,6).
- Vách trái đấu trường chỉ chạy sang trái `CliffReach` 14 (30 thì ló vào mép phải khung hố trên máy 20:9).
- Sprite mới từ ChatGPT: `Assets/Art/Reference/map_ui_kit.png` → `hud_pup`, `node_stone`, `node_star`, `node_glow`,
  `hud_bear` (thay bản cũ cùng vòng viền với hải cẩu), `ui_card` (panel trơn). Cắt bằng `Tools/cut_props.py`.

### R8. Thứ tự làm (mỗi bước: build + test + chơi trên máy ảo)
1. Tranh ChatGPT: nền hố bắn, máy bắn, ô đấu trường, khối, UI kit, bản đồ, menu, truyện, thắng/thua, hải cẩu và gấu frame mới.
2. Cắt tranh và nối vào game (đặt va chạm hố bắn theo tranh).
3. Luật pha 1 (R3), pha 2 (R4), kết quả/thống kê (R5), truyện (R6).
4. Mượt (R2): frame rate, squash/stretch, tween UI, camera.
5. Kiểm tra: EditMode + PlayMode, review theo khu vực thay đổi, APK, QA trên `Pixel_QA_34`.

### R9. Rà soát toàn dự án (15/11/2026, người dùng: "rà soát lại toàn bộ dự án … sửa và kiểm hết")
Luật áp dụng cho mọi màn hình và màn chơi từ nay:
- **Nút Back của Android hoạt động ở mọi màn hình**: trong màn chơi = tạm dừng/tiếp tục; thẻ kết quả = về bản đồ; shop, cài đặt,
  thống kê, hỏi xóa, cách chơi, giới thiệu, truyện = đóng cái trên cùng; bản đồ = về menu; menu = thoát game. Màn hình mới
  phải có `HandleBack()` và được gọi từ view gốc của scene.
- **Chạm**: mọi ngón tay đều tính (`GameInput.ActionPressed` duyệt hết `touches`), không chỉ ngón đầu tiên.
- **Cụm nút tròn trên-giữa** cách nhau đúng `UiKit.MinHit` (130) để vùng chạm không chồng nhau. Nhãn nút chừa 20 px mỗi đầu.
- **Shop**: 3 thẻ nằm trong ±800 (vừa 4:3), dùng safe area; nút giá chỉ ghi số + sao; chưa đủ sao thì nút xám + ổ khóa, bấm vào
  hiện "Cần thêm N sao". Nút Nâng cấp trên bản đồ có huy hiệu sao ghi số sao còn tiêu được, nhấp nháy khi mua được gì đó.
- **Thẻ kết quả**: bỏ dòng "Tổng gấu" (vẫn có ở thẻ nhiệm vụ và trang Thống kê) để lấy chỗ ghi luật dưới 3 sao; mỗi sao nằm trên đúng luật của nó (sao giữa có thể trống khi sao thứ ba đạt; ở màn boss luật sao giữa ghi "Hạ trùm khi còn dư hải cẩu"). Thắng màn boss
  ghi câu kết. Màn bonus ghi "Cá đã gom x/y".
- **Màn 1–3 là màn dạy** (`tutorial: true`): màn 3 cần 3 gấu (trước là 4 và tắt trợ giúp). Màn dạy vẽ 8 chấm theo đường lao
  chéo từ chỗ hải cẩu đang bay; chữ gợi ý là "Chạm sớm! Hải cẩu lao chéo về phía trước".
- **Nút "?" trong màn chơi** chỉ hiện gợi ý của pha hiện tại.
- **Hiệu năng**: texture Android nén ASTC (tranh nền 6×6, sprite và UI 4×4); bản đồ (màn tĩnh) chạy 30 fps và không dựng
  backdrop dưới `map_bg`; tranh truyện được giải phóng sau khi xem (`Art.Release`).
- **Quảng cáo**: chỉ bản cài từ Google Play mới dùng ad unit thật (`Application.installerName`); mọi bản khác dùng unit test.
  Nội dung quảng cáo giới hạn ở mức PG.
- **Chơi lại trên thẻ thua = chỉ pha tấn công** (người dùng 15/11: "làm những j chưa làm đi"): nút vàng ghi "Tấn công lại",
  vào thẳng đấu trường với đúng những hải cẩu đã thoát lần đó (cùng độ cao và tốc độ bay vào), không qua hố bắn và thẻ nhiệm
  vụ. Quảng cáo +1 hải cẩu cũng vậy, thêm 1 con. Khi đó thẻ thua có 4 nút: Bản đồ (chỉ còn biểu tượng) · Chơi lại (cả màn từ
  hố bắn, để người thoát được quá ít hải cẩu không bị kẹt) · Quảng cáo: +1 hải cẩu · Tấn công lại (vàng). Không con nào
  thoát thì chỉ có "Chơi lại" cả màn. Chơi lại ở menu tạm dừng và thẻ thắng luôn là **cả màn** từ hố bắn (`GameManager.Retry` so với `RetryLost`, `SceneLoader.KeptExits`).
- **Cảm giác đánh trúng**: mỗi gấu ngã có một nhịp khựng (thời gian ×0,05 trong 0,06 s thực) và một rung nhẹ; con gấu cuối
  của màn (hoặc trùm) ngã chậm (×0,3 trong 0,45 s). Tạm dừng luôn thắng: nhịp khựng không bắt đầu hay kết thúc đè lên nó
  (`GameManager.TimeDip`). Rung dùng phản hồi chạm của Android (`Haptics.Tap`, không cần quyền, theo cài đặt máy); cũng rung khi bắn "TUYỆT!".
- **Phần thưởng quảng cáo**: sau khi quảng cáo đóng, game chờ 1 giây cho callback thưởng (trước là 0,3 s: callback chậm thì xem
  xong mà không được hải cẩu).
- Chưa làm, chờ người dùng: analytics (cần tạo project Firebase và sửa Data safety + chính sách riêng tư), thêm màn sau boss
  (cần tranh bản đồ trang 2).

## Mục lục

0. [Quan sát từ bản tham chiếu](#0-quan-sát-từ-bản-tham-chiếu-chơi-thật--xem-3-video-11112026)
1. [Tầm nhìn & trụ cột](#1-tầm-nhìn--trụ-cột)
2. [Thế giới & nhân vật](#2-thế-giới--nhân-vật)
3. [Vòng chơi](#3-vòng-chơi)
4. [Pha 1 · Hố bắn](#4-pha-1--hố-bắn)
5. [Pha 2 · Đấu trường](#5-pha-2--đấu-trường)
6. [Kỹ năng bay & nâng cấp](#6-kỹ-năng-bay--nâng-cấp)
7. [Vật thể trong đấu trường](#7-vật-thể-trong-đấu-trường)
8. [Màn chơi & độ khó](#8-màn-chơi--độ-khó)
9. [Điểm, sao, hoàn hảo](#9-điểm-sao-hoàn-hảo)
10. [Màn hình & trải nghiệm](#10-màn-hình--trải-nghiệm)
11. [Nghệ thuật](#11-nghệ-thuật)
12. [Âm thanh](#12-âm-thanh)
13. [Kỹ thuật](#13-kỹ-thuật)
13b. [Quảng cáo](#13b-quảng-cáo-admob-người-dùng-13112026)
14. [Kế hoạch sản xuất](#14-kế-hoạch-sản-xuất)
15. [Rủi ro](#15-rủi-ro)
16. [Quyết định cần bạn chốt](#16-quyết-định-cần-bạn-chốt)

---

## 0. Quan sát từ bản tham chiếu (chơi thật + xem 3 video, 11/11/2026)

Nguồn: tự chơi bản Flash (Snokido, 2 màn, mở Instructions/Pause/Help); xem khung hình theo mốc thời gian của 3 video:
bản Flash no-commentary 13 phút (11 màn đầu + 1 boss), bản **Android** v1.1.2 (menu, map, nâng cấp, 3 màn), bản Java
longplay 53 phút (đủ 3 vùng + boss cuối + kết thúc). Chỉ lấy **cách chơi, bố cục, nhịp, hoạt ảnh**; hình, tên, chữ là của mình.

### Luồng app (bản Android)
Splash logo (chim đội mũ bay ngang qua chữ) → **menu dạng danh sách** trên nền cam: Play Campaign, Play Strategy Game,
Instructions, High Scores, Settings, About; mũi tên lên/xuống cuộn danh sách, nút ✓ và Exit ở hai góc dưới → **bản đồ**
→ màn tải (gấu xách lồng có chim bị nhốt chạy ngang, thanh tiến độ) → pha 1 → pha 2 → thẻ kết quả → bản đồ. Bản Flash:
cảnh mở đầu chim bị nhốt lồng (có Skip), màn chào chỉ có Play, thanh trên 3 nút tròn (loa, nhạc, ?). Chuyển màn: logo +
**mặt nước dâng lên che màn** rồi rút xuống. Kết thúc game: "THE END", gấu và chim đứng trên tảng băng trôi.

### Bản đồ
- Flash: một đảo nhìn từ trên, ~45 nút; nút chưa chơi "?", đã qua **chấm xanh lá**, Perfect **sao vàng**; **lồng = boss**;
  chim đội mũ đứng nhún ở nút hiện tại; nút kế tiếp có chữ "HELP!" đỏ nhấp nháy khi là boss.
- Android/Java: bản đồ **cuộn được** theo các vùng (đảo băng → đảo cọ → núi lửa ở vùng cuối); dưới nút hiện **1–3 sao**;
  góc trên-phải `★ đã có/tối đa`; chú thích dưới: "Tap the Penguin to start a mission", "2 more stars to earn an upgrade!".

### Pha 1 · hố bắn (giống nhau ở mọi màn, chỉ đổi màu đá theo vùng)
- Khung: đá cuội vân băng bao quanh, trần nghiêng, nhũ băng; nền làng lều băng + mây hồng. Cầu dầm thép từ trái, chim
  xếp hàng trên cầu; **bảng số** (hiện số chim còn trên cầu) treo dưới mép cầu; dây buông từ mép cầu.
- Máy bắn: bánh xe gỗ, nồi hơi có ống khói, cần dài có **muỗng** chứa cầu tuyết; một chim đứng gạt cần cạnh máy.
- Chạm: cần vung, **cầu tuyết bay ra**; trúng chim thì chim bay vọt lên-phải, **chữ "OK!" cam** hiện ở chỗ trúng; trượt thì
  chữ **"Grr"** xanh lá (chim gạt cần bực). Chim thoát bay ra **góc trên-phải** (Flash: đáp gờ và xếp hàng chờ).
- Máy có **khói phụt** khi bắn; cầu tuyết vỡ bụi khi rơi xuống đáy.

### Pha 2 · đấu trường
- Bố cục màn: 1 màn rộng hơn màn hình một chút, **camera lia theo chim** (cả dọc và ngang). Địa hình: dốc, bệ, cột, hang,
  **tường/trần đá**, **khối băng** (lập phương, tam giác, gai), **sàn gỗ ván**, **cầu băng**. Vùng 1 hoàng hôn cam + biển băng;
  vùng 2 tuyết trắng + đá xám, trời vàng; vùng 3 **hang băng**: trần nhũ băng khổng lồ, nền cam đất, sao lấp lánh.
- HUD: trên-trái **khung gỗ có số chim còn**; trên-phải **mặt gấu + `đã hạ/cần`**; Flash thêm `Total` (tổng gấu cả game).
- Bay: chim bay ngang từ mép trái, **ra mép phải vòng lại mép trái**. Chạm: chim **cắm đầu, xoay**, rơi nhanh dần.
- Trúng gấu: gấu **ngã ngửa, sao choáng xoay**, chim **nảy lên** và rơi tiếp; nhiều gấu một lượt → chữ **×2, ×3, ×5** lớn
  màu nâu giữa màn. Gấu cầm **tô cá** ăn, có con **ngủ**, có con giơ tay. Gấu bị hạ nằm lại, không biến mất ngay.
- Trúng khối băng: **vỡ vụn** từng mảnh, chim xuyên tiếp. Trúng đá/gỗ: nảy. Đáp sàn: bụi tuyết, chim đứng dậy rồi đi ra.
- **Vòng bật cam** (bumper hình bánh răng): chạm vào là chim **bật ngược mạnh**, dùng để đánh tới gấu khuất.
- **Thùng TNT** đỏ: trúng thì nổ, phá khối và hất gấu xung quanh.
- **Nút đỏ/xanh** trên bệ: chim đáp vào nút → kích hoạt (bật gấu boss xuống, mở cửa băng, thả khối).
- Boss: gấu to trên bệ cao, **thanh máu dọc** mép phải; hạ bằng cách đáp nút để hất nó xuống nhiều lần; khi hết máu thì
  **lồng mở**, chim con nhảy ra. Lời nhắc dưới màn: "Hit the boss bear several times to knock it out".
- Kết quả: "PERFECT MISSION" + mặt gấu `X/Y` + **1–3 sao** (Android); "MISSION SUCCESSFUL" (Java); Flash: "Perfect!" + hình
  4 chim nhảy trên gấu nằm, "Bears hit X/Y", "Total bears".

### Kỹ năng (power-ups)
| Kỹ năng | Hình ảnh | Cách dùng |
|---|---|---|
| Twins | 2 chim song song | 1 lượt phóng ra 2 chim |
| Rubber Penguin | chim nảy như bóng | thêm số lần nảy ("Two more bounces") |
| Penguin on Fire | **đuôi lửa vàng-cam** khi bay | xuyên băng, mất lửa khi đụng vật cứng |
| Ninja Penguin | **bóng thoại tròn có hình chim ninja**, chim **khựng giữa không** | chạm lần 2: dừng rồi lao theo hướng mới |
Android: thẻ "Star Upgrade" sau khi đủ sao: danh sách kỹ năng, chọn một, dòng mô tả dưới, nút OK. Mỗi kỹ năng có cấp.

### Hoạt ảnh nhân vật thấy được
- Chim: xếp hàng nhún, đi trên cầu, đu dây (vây giơ lên ôm dây), bị đập bay xoay, bay ngang (vỗ cánh 2 frame), cắm đầu,
  cuộn (Rubber), bốc lửa, ninja khựng, đáp đất choáng rồi đứng dậy đi, nhảy mừng (thẻ kết quả), xoay 360° trên bệ (thẻ màn).
- Gấu: đứng thở, ăn tô cá (múc ăn), ngủ, giơ tay trêu, bị trúng ngã ngửa + sao, boss bị hất văng rồi trèo lại, xách lồng chạy.
- Hiệu ứng: chữ "OK!" / "Grr", khói máy, bụi tuyết, mảnh băng, sao choáng, chữ combo ×N, nổ TNT, mặt nước chuyển cảnh.

### Khác với game mình đang có
| Hạng mục | Tham chiếu | Game mình (build hiện tại) | Quyết định |
|---|---|---|---|
| Vật bắn | quả cầu tuyết văng từ máy, trúng chim | chày của máy đập quét trúng hải cẩu | **Theo tham chiếu** (người dùng 11/11: "bố cục, các thứ của CPC luôn"): cốc trên cần thả cầu tuyết ở 12°, cầu bay theo trọng lực 9, trúng thì hải cẩu bay theo hướng cầu; trượt thì cầu nổ bụi + "Grr…". Tường hố = đá cuội như đấu trường. HUD: cụm 4 nút tròn trên-giữa (loa, nhạc, ?, tạm dừng), điểm dưới bộ đếm gấu |
| Chim thoát | đáp lên gờ góc trên-phải, xếp hàng chờ | bay khỏi màn, chỉ đếm | **Làm gờ chờ**: hải cẩu thoát đáp xuống gờ, xếp hàng, thấy ngay số đạn |
| Pha 2 bay | ngang, lặp vòng từ mép trái | ngang, ra mép phải là mất | **Lặp vòng** 2 lần rồi mất (cho người mới thở) |
| Lao | cắm đầu rơi gần thẳng | lao chéo chậm 45° | **Theo tham chiếu**: cắm đầu, rơi nhanh dần, giữ 30% đà ngang (người dùng từng muốn "chéo, chậm": làm số điều chỉnh `diveKeepHorizontal`, `diveGravity` để thử cả hai) ❓ |
| Trúng gấu | nảy lên theo pháp tuyến, combo ×2 | đè bẹp, dừng | **Nảy + combo** |
| Camera pha 2 | lia theo chim, màn rộng hơn 1 màn | cố định | **Lia theo chim** (dọc + ngang nhẹ, màn rộng 1.3 màn hình) |
| Kỹ năng | gắn vào loại chim, mở theo màn | mua bằng sao | **Gộp**: loại hải cẩu mở theo màn, sao mua cấp |
| Kết quả | 1 thẻ, không sao | 3 sao | Giữ 3 sao + huy hiệu Perfect |
| Bản đồ | 45 nút, 5 boss, không tiêu đề | 10 nút, 1 vùng | Vùng 1: 10 + thưởng + boss; mở rộng dần |
| Phản hồi pha 1 | "OK!" khi trúng, "Grr" khi trượt, khói máy | chỉ bụi + rung | **Thêm** chữ OK/Grr và khói |
| Vật thể | vòng bật, TNT, nút, sàn gỗ, cầu băng | khối băng/tuyết, đá | **Thêm** vòng bật, TNT, nút (theo mốc M3) |
| Gấu | ăn tô cá, ngủ, trêu; bị hạ nằm lại | thở, trêu, lộn nhào mờ dần | **Thêm** ăn/ngủ, nằm lại tới hết màn |
| Boss | đáp nút hất boss, thanh máu dọc, lồng mở | chưa có | **Theo tham chiếu** (M4) |
| Vùng | hoàng hôn → tuyết xám → hang băng | 1 vùng | 3 vùng, đổi màu đá + nền |
| Chuyển cảnh | mặt nước dâng che màn | mờ dần | **Thêm** sóng nước che màn |

## 1. Tầm nhìn & trụ cột

| Mục | Nội dung |
|---|---|
| Một câu | Game canh nhịp + vật lý vui nhộn, mỗi màn 30–90 giây, chơi lại để lấy 3 sao |
| Người chơi | Mọi lứa tuổi, chơi bằng một ngón tay, quốc tế (EN mặc định, VI) |
| Nền tảng | Android, Google Play, màn hình ngang (cả hai chiều), 16:9 chuẩn, 4:3 hẹp nhất, 20:9 rộng nhất |
| Phiên chơi | 2–5 phút (3–5 màn), không cần mạng |
| Tiền | Miễn phí, có quảng cáo AdMob nhẹ tay (người dùng 13/11/2026: "đẩy dự án lên admob"), xem §13b |

**Trụ cột** (mọi quyết định đối chiếu với 4 điều này):
1. **Canh nhịp dễ hiểu, khó thành thạo:** một nút chạm cho cả hai pha; nhìn là biết, luyện mới giỏi.
2. **Phá phải đã tay:** khối văng, gấu lộn nhào, rung camera, điểm bay lên; vật lý thật nhưng hướng về niềm vui.
3. **Dễ thương, không bạo lực:** gấu bị "hạ" là lộn nhào choáng váng, không máu, không đau.
4. **Chơi lại có lý do:** sao, danh hiệu "Hoàn hảo", cá giấu, nâng cấp mở thêm cách chơi.

## 2. Thế giới & nhân vật

### Bối cảnh
Bầy gấu bắc cực chiếm Vịnh Băng Giá và giấu kho cá mùa đông của làng hải cẩu lên các bệ đá, cột băng và pháo đài khắp
vịnh. Ông Hải Mã chế máy đập đặt trong hố băng; bầy hải cẩu con xếp hàng, từng con đu dây, chờ ông đập cho bay vọt qua
khe thoát sang đấu trường của gấu. Cuối mỗi vùng, Vua Gấu Băng nhốt vài hải cẩu con trong lồng; thắng boss thì cứu được.

### Nhân vật
- **Pip và bầy hải cẩu con** ✅ (anh hùng): lông trắng, mắt to, mũ len đỏ quả bông, kính phi công, khăn xanh ngọc. Đi bằng
  hai chân sau, lao xuống thì cuộn thành quả bóng. Vẽ frame-by-frame bằng ChatGPT (sheet trong `Assets/Art/Reference/`,
  cắt bằng `Tools/slice_sheets.py`, hải cẩu đứng cao 300 px, tỉ lệ đo theo mũ đỏ). Bộ hoạt ảnh: idle 4, walk 6, leap 4,
  hang 4 (có dây + nút thắt nối tiếp dây game), flail 4, cheer 4, glide 4, dizzy 4, ball 1 (xoay bằng code), lever 8,
  launch 4 (bay chéo 35°), fall 4. Sheet mới phải dùng **Chỉnh sửa** trên sheet cũ để giữ nét (sheet tạo mới bị lệch nét).
- **Hải cẩu gạt cần** ✅: một con đứng sau đuôi xe trượt, gạt cần khi người chơi chạm (frame lever).
- **Ông Hải Mã** 🆕 (chỉ trong truyện/giới thiệu, không có trong màn chơi; có thể xuất hiện ở màn giới thiệu vùng sau).
- **Gấu bắc cực** 🔧 (kẻ địch; người dùng 11/11: thay cua bằng gấu): đứng trần trên đỉnh dốc, cột, chồng khối; thỉnh thoảng
  giơ hai tay trêu. Bộ hoạt ảnh: idle 4 (thở + chớp mắt), taunt 4, dizzy 4 (mắt xoáy, sao), ko 4 (nằm ngửa mắt X); hit = 2
  frame đầu của dizzy. Vẽ bằng ChatGPT theo `docs/bear-art-brief.md` → `bear_sheet.png` → `slice_sheets.py` (hiện là frame
  tạm vẽ bằng code, cùng tên file nên thay là xong). Loại **đội mũ băng** chịu 2 đòn (mũ là sprite rời `bear_hat`).
- **Vua Gấu Băng** 🆕 (boss cuối vùng): to gấp đôi, 8 máu, đứng trên pháo đài nhiều tầng, mỗi đòn trúng thì rụng một mảng
  áo giáp băng; có lồng nhốt hải cẩu con bên cạnh.
- **Hải cẩu con bị nhốt** 🆕: trong lồng ở màn boss và hiện trên bản đồ tại nút boss (giống ảnh mẫu #14).
- **Đàn nhạn biển** 🆕 (vùng 2+): bay cắt ngang pha 2, chạm thì hải cẩu đổi hướng.

### Cảm giác
Vui, lộn xộn, "póc" khi băng vỡ, bụi tuyết tung, gấu lộn nhào kêu "oof", sao bay. Hoàng hôn cam trên biển băng ở đấu
trường tương phản với đêm cực quang ở hố bắn và menu.

## 3. Vòng chơi

```
Bản đồ (chọn màn) → Màn chơi: Pha 1 (bắn N hải cẩu) → Chuyển cảnh (camera lia) → Pha 2 (M hải cẩu đã thoát tấn công)
→ Kết quả (sao, Hoàn hảo, cá) → Bản đồ (mở màn kế / nâng cấp)
```
- Số hải cẩu mỗi màn N = 5–8 (+ nâng cấp Bầy). Chỉ con **thoát được** hố mới được tấn công; con bay hỏng là mất.
- Thắng khi hạ hết gấu. Thua khi hết hải cẩu mà gấu còn. Còn dư hải cẩu = thưởng sao.
- Sao mua nâng cấp. Danh hiệu Hoàn hảo và cá giấu là mục tiêu chơi lại.

## 4. Pha 1 · Hố bắn

✅ Đã có trong build, bố cục ngang theo ảnh mẫu #4. Toạ độ thế giới, sàn `Backdrop.GroundY` = −4.5, tâm nhìn (0, 0.1),
camera cao 10 đơn vị (`orthographicSize` 5; màn 4:3 nới cao để luôn thấy 14.2 đơn vị ngang).

| Phần | Vị trí / số | Ghi chú |
|---|---|---|
| Trần đá + nhũ băng | y = 4.6, từ mép trái tới x = 2.6 | nhũ băng lát theo ô 4.35, ô cuối cắt đúng mép |
| Tường trái / phải | x < −6.3 / x > 6.3, tường phải chỉ cao tới y = 4.0 | khe thoát = góc trên-phải |
| Cầu dầm | y = 2.1, từ tường trái tới x = −0.4 | hải cẩu xếp hàng, bước 0.6, không cần tầng 2 |
| Bảng đếm | (−2.7, 1.35) | lùi trái để dây đu hết biên không quét qua |
| Dây | neo (−0.65, 2.16), dài 2.4; đáy cung y ≈ −0.83 | biên độ mỗi lượt ngẫu nhiên 24°–40° |
| Máy đập | hub trên cung chày đi qua đáy cung đu; mặt bệ y ≈ −3.2 | quay mặt vào hố; hải cẩu gạt cần cách tâm máy 1.75 |

**Gờ chờ** 🆕 (theo tham chiếu): hải cẩu lọt khe đáp xuống một gờ đá ở góc trên-phải (ngoài tường phải, y ≈ 4.2), hoạt ảnh
hạ cánh + đứng xếp hàng (idle/cheer), hàng dài theo số con thoát; thẻ chuyển pha hiện số con trên gờ.

**Luật:** chạm → cần gạt, chày quét lên theo chiều kim đồng hồ 0.24 s. Chày chạm hải cẩu thật (test tròn-tròn, chia nhỏ
bước) thì hải cẩu bay theo pháp tuyến va chạm + đà đu (`swingInfluence` 0.6) + đà chày, nắn nhẹ về `strikeAngle` 48°
(`aimAssist` 0.4), kẹp 20°–100°, tốc độ `strikePower` 13. Lọt khe (40°–52° từ đáy cung, xem R3) → bay tiếp sang pha 2. Đụng tường/trần/cầu →
choáng, rơi khỏi màn, mất. Đập trượt → đu tiếp, không mất gì. Hải cẩu bay nghiêng theo hướng bay (frame launch).

**Nhịp:** đập khi hải cẩu ở đáy cung hoặc vừa qua trái → lọt; đập sớm (còn bên phải) → thấp, đụng tường phải; đập muộn
(đang lên cao bên trái) → dốc, đụng trần. Gợi ý ở màn 1–2: "Chạm để đập! Đập lúc nó đu xuống thấp nhất".

🔧 Cần làm thêm: hoạt ảnh ông Hải Mã? (không — giữ hải cẩu gạt cần); hiệu ứng chày trúng (bụi tuyết, "bốp") ✅; sao trail ✅.

## 5. Pha 2 · Đấu trường

✅ Bố cục theo ảnh mẫu #5/#6/#10/#12/#13 (xem kỹ 11/11) và chơi lại 12/11 đêm (báo cáo §14.1): trời hoàng hôn cam nửa
trên, dải đồi cam, chân trời núi băng, biển băng vụn nửa dưới; sàn đá cuội mỏng; **vách đá cuội có mũ tuyết bên TRÁI**
(khối cao 4.2 tới x = −1.2, dốc 1.8 xuống sàn ở x = 0.6), **bên phải mở**, không tường. Đấu trường rộng 13 đơn vị, gốc
(40, `GroundY`+0.8), tâm nhìn gốc+(6.0, 3.7) (lệch trái 0.5 để thấy vách, gờ xa bên phải bị cắt nửa như CPC), camera thu
10% (`AttackZoom` 0.9), cố định. Khối băng vỡ để lại mảnh (`Fx.Rubble`) đến hết màn.

| Thành phần | Mô tả | Trạng thái |
|---|---|---|
| Dốc đá (`Ramp`) | tam giác vuông, nghiêng trái/phải, `rock_ramp` kéo giãn, `PolygonCollider2D` | ✅ |
| Bệ đá (`Rock`), cột đá (`Pillar`) | chữ nhật, `rock_cobble` lát + viền tuyết `rock_top` | ✅ |
| Khối băng lập phương (`Cubes`) | 0.8×0.8, vật lý thật, vỡ khi va mạnh (`iceHp` 7), xếp chồng | ✅ |
| Khối tuyết | vỡ dễ (`snowHp` 2), bụi tuyết | ✅ |
| Gấu | đứng trần trên đỉnh; hạ khi va ≥ `bearHitImpulse` 3 hoặc rơi ≥ 1.1; bị hạ thì **văng ngược xa** (`KnockUp` 7, ngang 1.8–3) | ✅ |
| Gờ băng nổi (`IceShelf`) | tấm băng dày 0.7 có mũ tuyết, lơ lửng, cứng; `EdgePerch` đặt ở x 13.6 (ngoài sân 13) với một gấu, bị mép màn cắt nửa như CPC (báo cáo §14.1); màn 1, 3, 7 | ✅ |

**Bay (theo tham chiếu):** hải cẩu vào từ mép trái gần đỉnh màn, **bay ngang không rơi** (4.5 đơn vị/s, ~3 s/màn), vỗ
vây 2 frame; ra mép phải thì **vòng lại từ mép trái** (tối đa 2 vòng, vòng 3 thì rơi mất: `maxLoops` 🆕). **Chạm:** cuộn
tròn + **quay đầu xuống** 0.2 s rồi rơi nhanh dần (`diveGravity` 0.9 🔧, giữ 30% đà ngang `diveKeepHorizontal` 0.3 🔧; vệt
mờ). **Trúng gấu:** gấu lật 90° bay ra sau + sao choáng + "+1"; hải cẩu **nảy lên theo pháp tuyến** (`bounceKeep` 0.75) và
rơi tiếp; trúng con thứ hai trong cùng lượt → "×2" lớn giữa màn, ×3… (combo 🆕, điểm nhân). **Trúng khối băng hoặc tuyết:** vỡ mảnh,
xuyên tiếp (chậm 15%). **Trúng đá:** nảy, không phá. **Đáp đất:** bụi tuyết, đứng dậy **đi bộ ra mép** rồi biến mất 🆕
(frame walk), con kế bay vào. **Camera** lia theo hải cẩu theo trục dọc (và ngang nhẹ) trong đấu trường rộng 1.3 màn 🆕.
Hạ đủ mục tiêu vẫn chơi tiếp các con còn lại để lấy Perfect (hạ **tất cả** gấu) 🆕.

🆕 Cần làm: **vòng tăng tốc** (hoop, ảnh #9), **lửa** (ảnh #10), **đổi hướng ninja** (ảnh #13) — xem §6; **cá giấu** và
**lồng** — xem §7.

## 6. Kỹ năng bay & nâng cấp

Tham chiếu gắn kỹ năng vào **loại chim** trong hàng (mũ khác nhau), mở theo màn, không mua. Game mình gộp: **loại hải cẩu**
(mũ len màu khác) xuất hiện trong hàng từ màn mở khoá, mỗi loại có kỹ năng riêng tự kích hoạt; **sao mua cấp** để kỹ năng mạnh
hơn (3 cấp). Hàng chờ hiện rõ mũ nào sắp tới lượt nên người chơi lên kế hoạch được.

| Kỹ năng | Tác dụng trong pha 2 | Cách dùng | Mở ở | Trạng thái |
|---|---|---|---|---|
| Gai băng (Ice Spikes) | xuyên 1/2/3 khối băng khi lao | tự động | màn 3 | ✅ |
| Cuộn tròn (Bouncy) | nảy thêm 1/2/3 lần sau va chạm | tự động | màn 2 | ✅ |
| Bầy hải cẩu (Pod) | +1/+2/+3 hải cẩu mỗi màn | tự động | màn 4 | ✅ |
| Chân chèo lái (Steer) | đổi hướng trên không 1/2/3 lần | vuốt trái/phải | màn 6 | ✅ (chưa có hướng dẫn) |
| Vòng băng (Boost Ring) 🆕 | vòng đặt trong màn; bay qua vòng → tăng tốc, bay thẳng tiếp 3 đơn vị | bay qua | màn 5 (vật thể màn) | 🆕 |
| Cầu lửa (Blaze) 🆕 | chạm lần 2 khi đang lao → bốc lửa, xuyên mọi khối băng cho tới cú va đá/gấu; mất lửa khi va | chạm lần 2 | màn 7 | 🆕 |

❓ Có giữ Steer (vuốt) không, hay gộp vào "đổi hướng ninja" (chạm lần 2 khi đang lao → quay 90° xuống thẳng)? Một ngón tay
chạm nhất quán hơn vuốt.

Giá (sao) cấp 1/2/3: Gai 1/2/3 · Cuộn 1/2/3 · Bầy 1/2/3 · Ninja 2/2/3 · Lửa 2/2/3 = 32 sao cho cả shop; 12 màn cho tối đa
36 sao (R9, 15/11: giá cũ cộng lại 117 sao nên không bao giờ mua nổi 1/3 shop; test `EconomyTests` giữ tổng giá ≤ số sao có trong game).

## 7. Vật thể trong đấu trường

| Vật thể | Mô tả | Trạng thái |
|---|---|---|
| Cá giấu 🆕 | **Chưa làm** (15/11: code chỉ có cá ở màn bonus; Hoàn hảo = hạ hết gấu, và gom đủ cá ở màn bonus). Ý tưởng cũ: 3 con cá mỗi màn đặt ở chỗ khó (sau cột, trên cao); bay/lăn qua là nhặt | 🆕 |
| Vòng băng 🆕 | xem §6 | 🆕 |
| Thùng gỗ 🆕 | khối nặng (`stoneHp` 22), không vỡ dễ, dùng làm bệ như ảnh #12 | 🆕 (sprite có `block_stone`) |
| Lồng băng 🆕 | ở màn boss, treo cạnh boss; thắng boss thì mở, hải cẩu con nhảy ra | 🆕 |
| Nhũ băng rơi 🆕 (vùng 2) | va vào trần đấu trường làm nhũ rơi xuống gấu | 🆕 |

## 8. Màn chơi & độ khó

**Cấu trúc:** 3 vùng × (10 màn + 1 màn thưởng + 1 boss). MVP = vùng 1 đủ 10 màn ✅ + màn thưởng 🆕 + boss 🆕.

| Vùng | Chủ đề | Cái mới | Trạng thái |
|---|---|---|---|
| 1. Vịnh Băng Giá | hoàng hôn, biển băng | dốc, cột, khối băng, gấu mũ băng, vòng băng (màn 5), lửa (màn 7) | ✅ 10 màn (chưa có vòng/lửa) |
| 2. Sông Băng | ban ngày xanh | thùng gỗ, nhũ băng rơi, nhạn biển, khối trượt | 🆕 |
| 3. Đêm Cực | đêm cực quang | tảng băng trôi di chuyển, gió ngang, boss cuối | 🆕 |

**Màn thưởng** (ảnh #11): không có gấu; 24 con cá rải trên không; mỗi hải cẩu bay qua nhặt; điểm = số cá; không mất sao.
**Màn boss** (ảnh #12): Vua Gấu Băng 8 máu trên pháo đài 3 tầng + lồng; nút boss trên bản đồ có hình hải cẩu bị nhốt (ảnh #14).

**Vùng 1 — tóm tắt 10 màn** (chi tiết toạ độ trong `Assets/Editor/LevelDesigns.cs`):

| Màn | Hải cẩu | Gấu | Địa hình | Dạy gì |
|---|---|---|---|---|
| 1 | 5 | 1 | đồi giữa (dốc–bệ–dốc) | đập nhịp, chạm lao chéo |
| 2 | 5 | 2 | đồi thấp trái, bệ cao phải | chạm sớm khi mục tiêu cao |
| 3 | 6 | 2 | gấu trên cột khối băng + gờ phải | phá khối băng để gấu rơi |
| 4 | 6 | 3 | cột đá cao (gấu mũ băng), bệ + khối | gấu 2 đòn |
| 5 | 6 | 3 | dốc dài, cột khối giữa, bệ cao phải | 🆕 vòng băng đầu tiên |
| 6 | 7 | 3 | gờ dài, 3 tháp khối băng | chọn thứ tự mục tiêu |
| 7 | 7 | 4 | đồi lớn + gấu dưới sàn | 🆕 cầu lửa xuyên tháp |
| 8 | 7 | 3 | 3 cột cao thấp, tháp khối giữa | cột hẹp cần lao chính xác |
| 9 | 8 | 3 | tháp đôi trên bệ, gấu sát tường | nảy khỏi tường phải |
| 10 | 8 | 5 | pháo đài khối băng trên bệ rộng + 2 cột gác | tổng hợp |
| Thưởng | 6 | 0 | cá rải trên không | 🆕 |
| Boss | 8 | Vua + 2 | pháo đài 3 tầng, lồng | 🆕 |

Độ khó tăng bằng: số gấu, độ cao/độ hẹp của bệ, gấu mũ băng, ít hải cẩu dư. Mỗi màn chơi thử phải thắng được bằng "bot"
trong test tự động (`FullLevelReachesAResult`) và bằng tay trên máy ảo.

## 9. Điểm, sao, hoàn hảo

- Điểm: tuyết 50, băng 120, thùng 200, gấu 500, cá 100 (🆕), còn dư hải cẩu +200/con (🆕).
- Sao: ★ qua màn (hạ đủ số gấu yêu cầu) · +★ còn dư ≥ 1 hải cẩu (chỉ xảy ra khi hạ hết gấu trước con cuối) · +★ điểm ≥ 60% tổng điểm phá được của màn (tự tính khi dựng màn). Thẻ kết quả ghi luật dưới từng sao (R9).
- Màn bonus (cá) luôn dùng hết hải cẩu nên chấm theo cá: ★ chơi xong · +★ gom ≥ 50% cá · +★ gom ≥ 80% cá (`StarRating.ComputeBonus`).
- **Hoàn hảo** 🆕 (ảnh #7 "PERFECT MISSION"): hạ hết gấu **và** nhặt đủ 3 cá → huy hiệu vàng trên nút màn, +1 sao thưởng (❓).
- Lưu: sao/màn, huy hiệu/màn, cấp nâng cấp, cài đặt, ngôn ngữ (JSON, có kiểm tra từng trường, lỗi thì về mặc định).

## 10. Màn hình & trải nghiệm

Canvas 1920×1080, `matchWidthOrHeight` 0.5. Quy ước góc (R7e, 12/11): **trên-trái** mặt hải cẩu + "× N"; **trên-giữa** 4
nút tròn (loa, nhạc, ?, tạm dừng); **trên-phải** mặt gấu + Tổng + `đã hạ/cần` (pha 2); **dưới-giữa** thanh chú thích một
dòng. Không có điểm trên HUD. Nút ≥ 90 đơn vị canvas (~48 dp). Panel nằm ngang, nút xếp hàng.

| Màn hình | Nội dung | Trạng thái |
|---|---|---|
| Khởi động | logo, nạp save, ≤ 2 s | ✅ |
| Menu chính | sân khấu trái (hải cẩu bay vệt sao, máy đập, gấu trên khối băng), logo + cột nút phải | ✅ |
| Bản đồ | đảo 1920×1080, 10 nút hình S (hàng dưới 1→6, hàng trên 7→10), nút thưởng + boss có lồng 🆕, dấu chân, hải cẩu đứng ở màn kế, huy hiệu Hoàn hảo 🆕 | ✅ (thiếu thưởng/boss/huy hiệu) |
| HUD | như quy ước góc; gợi ý chạm ở màn 1–2 | ✅ |
| Tạm dừng | 2×2: Tiếp tục, Chơi lại, Bản đồ, Âm thanh | ✅ |
| Kết quả thắng | dải tiêu đề, 3 sao lần lượt, điểm, +sao, "Hoàn hảo!", cá 🆕; Bản đồ · Chơi lại · Màn tiếp | ✅ (thiếu cá) |
| Kết quả thua | hải cẩu choáng; Bản đồ · Chơi lại (cả màn) · Quảng cáo: +1 hải cẩu · Tấn công lại (nút chính, vàng; xem R9) | ✅ |
| Cửa hàng | thẻ 2×2 (→ 2×3 khi thêm Lửa 🆕), icon · tên · 3 viên cấp · Mua + giá; thanh mô tả | ✅ |
| Cài đặt | nhạc, hiệu ứng, ngôn ngữ, xoá dữ liệu (xác nhận), credits | ✅ |
| Cách chơi | 4 bước 2×2 | 🔧 cập nhật chữ theo gấu/lao chéo |
| Giới thiệu vùng 🆕 | thẻ 1 ảnh + 1 câu khi vào vùng mới | 🆕 |

Hướng dẫn bằng hình (icon chạm nhún) + một câu; chữ ít để dịch nhanh. Mọi chữ qua `Loc.cs` (EN/VI).

## 11. Nghệ thuật

**Phong cách:** vector mềm, viền navy `#16304f` ~5 px, chuyển màu nhẹ, highlight trắng; nhân vật chibi mắt to. Hai bảng
màu: đêm cực quang (menu, hố bắn: xanh đêm, băng, cực quang lục) và hoàng hôn (đấu trường: cam `#ff8a3d`→vàng→kem, biển
băng `#a9d8f0`). Cam/đỏ (mũ, lửa, vòng) là màu nhấn.

**Pipeline:** nhân vật (hải cẩu, gấu, boss) = sheet ChatGPT 4×4 → `slice_sheets.py` (brief trong `docs/`). Vật thể, nền,
UI = SVG trong `Tools/make_art.py` → `rsvg-convert` → `Assets/Resources/Art/` (không xoá `pup_*`; `bear_*` từ sheet sẽ bị
make_art ghi đè chừng nào `BEAR_ANIMS` còn trong `ASSETS` → khi có sheet, bỏ các mục `bear_*` khỏi `ASSETS` ❗).

**Thiết kế hoạt ảnh** (theo tham chiếu §0; bộ ✅ đã có sheet, 🆕 cần vẽ theo `docs/pup-art-brief-2.md` / `docs/bear-art-brief.md`, code đã nhận và dùng bộ cũ thay thế khi chưa có):

| Nhân vật | Bộ | Frame · fps | Khi nào | Trạng thái |
|---|---|---|---|---|
| Hải cẩu | idle / walk / leap / hang / flail / cheer / dizzy / ball / lever / launch / fall / glide | 4·3, 6·theo bước, 4·10, 4·5 (nhanh theo đu), 4·10, 4·8, 4·6, 1, 8·3/14, 4·10, 4·8, 4·8 | hàng chờ, đi trên cầu, bật chụp dây, đu, bị đập bay, mừng trên gờ, choáng, cuộn rơi, gạt cần, bay chéo, rơi khỏi hố, dự phòng bay | ✅ |
| Hải cẩu | **fly** 8·9 | bay ngang qua đấu trường, vây vỗ như ngư lôi | ✅ sheet E (ChatGPT 11/11) |
| Hải cẩu | **dive** 2·10 + xoay −90° trong 0.2 s rồi cuộn (ball) | chạm lần 1 | ✅ sheet E |
| Hải cẩu | **ninja** 1 (trong bong bóng `bubble_ninja`, khựng 0.25 s) | chạm lần 2 có Ninja | ✅ sheet E |
| Hải cẩu | **getup** 4·7 (nằm → chống tay → ngồi → đứng) rồi walk đi ra + mờ dần | nằm yên 0.8 s sau khi đáp | ✅ sheet E |
| Hải cẩu | lửa = ball + vệt tia lửa cam mỗi 0.025 s | Blaze | ✅ (hiệu ứng code) |
| Gấu | idle 4·3 (thở, chớp) / taunt 4·7 / dizzy 4·6 (+2 frame đầu = hit) / ko 4·5 (nằm, mắt X) | đứng gác, trêu, bị trúng, bị hạ nằm lại (frame vẽ nằm sẵn; gốc không xoay, collider nằm ngang) | ✅ `bear_sheet.png` (ChatGPT 11/11, gấu phe ác: cau có, nanh, sẹo, bụng phệ — người dùng: "gấu bên phe ác, nhìn hơi trẻ con" → vẽ lại) |
| Gấu | **eat** 4·4 (tô cá) / **sleep** 4·2 (z bay) | tâm trạng `BearMood` theo màn | ✅ `bear_sheet_b.png` |
| Vua Gấu | dùng bộ gấu ×2 cỡ; bay khỏi bệ (dizzy) → đáp (-1 mạng) → trèo lại (taunt, cung 1.4 s) → idle; hết mạng = ko | nút sàn cùng nhóm | ✅ code, art = gấu |
| Vật thể | vòng bật xoay 40°/s + giật khi trúng; TNT nổ (tia lửa + bụi + rung); nút lún khi đè; cá lắc ±12°; lồng đu ±4°, mở cửa + hải cẩu nhảy ra | | ✅ |
| Hiệu ứng chữ | "OK!" cam tại hải cẩu, "Grr…" xanh tại hải cẩu gạt cần, "+500"/"+100", "×N" nâu lớn giữa màn, "-1" đỏ ở boss | | ✅ |
| Chuyển cảnh | nước dâng 0.4 s (ease) → gấu xách lồng chạy ngang nhún trong lúc tải (≥ 0.5 s) → nước rút 0.45 s | mọi lần đổi scene | ✅ |

**Danh sách asset:**

| Nhóm | Có ✅ | Cần làm 🆕 |
|---|---|---|
| Hải cẩu | 12 bộ frame | — |
| Gấu | frame tạm (idle/taunt/dizzy/ko), mũ băng | sheet ChatGPT; Vua Gấu Băng (sheet riêng, 2× to); hải cẩu trong lồng |
| Hố bắn | tường/trần đá 9-slice, nhũ, cầu dầm, dây, bảng đếm, máy đập + cần | lớp đá có vân/nhũ phong phú hơn (hiện phẳng) 🔧 |
| Đấu trường | trời hoàng hôn, dải đồi + chân trời + biển băng, đá cuội lát, viền tuyết, dốc, khối băng/tuyết/đá | vòng băng, cá, lồng, thùng gỗ texture, nhũ rơi |
| Bản đồ | đảo vùng 1 | nút thưởng/boss, huy hiệu Hoàn hảo, đảo vùng 2–3 |
| UI | nút 3 màu, pill, panel, ribbon, icon 4 nâng cấp, glyph | icon Lửa, icon cá, huy hiệu |
| Hiệu ứng | bụi tuyết, mảnh băng, sao, vệt, tuyết rơi | lửa (trail), vòng sáng khi qua vòng |

## 12. Âm thanh

Hiện: tiếng tổng hợp từ `Tools/make_audio.py` (click, launch, boing, thud, pop, sand, dive, puff, star, win, lose, zap,
bear_down, nhạc nền `music_reef`). 🔧 Thay bằng bộ CC0 (Kenney) + nhạc nền riêng cho đấu trường (nhịp nhanh hơn) và menu;
🆕 tiếng gấu "oof", tiếng vòng băng, tiếng lửa, tiếng nhặt cá; ghi nguồn trong Cài đặt.

## 13. Kỹ thuật

Unity 6.3 LTS (6000.3.28f1), URP 2D, Physics2D, Cinemachine (lia + rung), Input System, uGUI + TMP (Baloo 2), lưu JSON.
Mọi màn hình dựng bằng code (`UiKit`), không prefab; mọi số "cảm giác" trong `Assets/Data/Tuning.asset`; màn trong
`LevelDesigns.cs` → `Level_XX.asset` (`RebuildLevels` sau khi sửa). Kiến trúc: `Core` (GameManager, Backdrop, Fx, Loc,
Save, PupRig), `Launch` (LaunchPhase, SwingRope, WhackerMachine), `Attack` (AttackPhase, PufferController, Block, Bear,
BearRig), `Progress` (data), `UI`. Test: EditMode (sao, cửa hàng, save) + PlayMode (màn đứng yên, lao phá, chơi hết một
màn bằng bot, quét nhịp đập, quay frame, chụp menu). Build: `ProjectBuilder` (APK test, AAB ký bằng biến môi trường).
Máy: M1 8 GB, ổ trống ≥ 10 GB; QA trên AVD `Pixel_QA_34` (chụp qua `adb emu screenrecord screenshot`).

## 13b. Quảng cáo (AdMob, người dùng 13/11/2026)

Lấy theo quy tắc của one-fill-line (`docs/BAO-CAO-KHAO-SAT.md` §6b ở repo đó): quảng cáo **nhẹ tay**, không bao giờ
chen giữa màn, mỗi quy tắc chặn đúng một lời chê hay gặp ở thể loại.

| Vị trí | Quy tắc | Chặn lời chê nào |
|---|---|---|
| Banner | **Không có.** Game ngang, hành động: banner che sân chơi và gây chạm nhầm | Banner che, chạm nhầm |
| Interstitial | Chỉ khi bấm **Tiếp tục** trên thẻ thắng, trước khi sang màn sau. Tối đa **1 lần mỗi 4 màn thắng**; cách mọi quảng cáo toàn màn **≥ 180 giây**; chưa có khi người chơi mới qua dưới **3 màn**; không có sau màn thưởng và màn boss (thẻ kết thúc chặng), không có khi bấm Chơi lại hay Bản đồ | "Quảng cáo sau mỗi màn", quảng cáo ngay sau phần hướng dẫn |
| Rewarded | Chỉ khi người chơi tự bấm nút có huy hiệu ▶: trên **thẻ thua**, nút "QC: +1 hải cẩu ▶" (tiếng Việt viết tắt cho vừa nút; ngôn ngữ khác ghi đủ chữ quảng cáo, vd. "Ad: +1 pup"); nút vàng chính luôn là lượt chơi lại miễn phí (R9) — chơi lại đúng màn đó với thêm 1 hải cẩu, dùng **một lần** (màn kế hay lần chơi lại sau đó trở lại bình thường) | Ép xem video; thua mãi là kẹt |
| Khi quảng cáo lỗi / không mạng | Rewarded: hiện "Chưa có quảng cáo", **không tặng gì**. Interstitial: bỏ qua, sang màn luôn | Tắt mạng để lấy quà miễn phí; game treo chờ quảng cáo |
| Âm thanh | Âm thanh game tắt (nút loa, menu tạm dừng, Cài đặt) thì quảng cáo cũng tắt tiếng (`MobileAds.SetApplicationMuted`) | Quảng cáo kêu to khi đã tắt âm |
| Đồng ý (GDPR) | Google UMP hỏi khi mở game, **trước** `MobileAds.Initialize`; không được phép thì chơi không quảng cáo. Cài đặt có dòng **Quyền riêng tư** khi UMP yêu cầu | Luật EU, chính sách Play |

Kỹ thuật: plugin Google Mobile Ads Unity (OpenUPM `com.google.ads.mobile`); quy tắc nằm ở lớp C# thuần
`AdPolicy` (test EditMode), phần gọi SDK ở `Ads`. Bản test/QA và máy ảo luôn dùng **ID quảng cáo test** của Google;
chỉ bản AAB phát hành dùng ID thật trong `AdsConfig.cs` (ID còn là chỗ trống thì vẫn dùng ID test và log lỗi, không
crash). Đếm "4 màn thắng" và "180 giây" theo phiên chơi (mở lại game thì đếm lại); "3 màn" đếm theo save (màn có ≥ 1 sao).

## 13c. Ngôn ngữ và đánh giá trong app (người dùng 13/11/2026: "làm SEO thật tốt, ưu tiên EU")

- **7 ngôn ngữ**: en, vi, de, fr, es, it, pl (cột theo thứ tự `Loc.Codes`). Lần đầu theo ngôn ngữ máy, không khớp thì
  English. Nút Ngôn ngữ trong Cài đặt **xoay vòng** en → vi → de → fr → es → it → pl, hiện tên của chính ngôn ngữ đó
  ("Deutsch", "Polski"). Tên nâng cấp, vùng và huy hiệu dịch **đúng bảng** trong `docs/ASO.md` (mục Châu Âu), để ảnh store
  và mô tả nói cùng một chữ. Giọng thân mật (du / tu / tú). Chữ dài hơn khung thì tự co tới 65% cỡ chữ (`UiKit.Text`),
  không tràn. Bản dịch do Claude viết, chưa có người bản xứ đọc lại.
- **Đánh giá trong app** (Google Play In-App Review, như one-fill-line `docs/ASO.md` §5): hỏi **một lần**, 1,5 giây sau khi
  thẻ thắng hiện ở lần thắng đầu tiên từ **màn 3** trở đi (15/11: trước là màn 5, tức ngay sau màn có thể hiện interstitial đầu tiên; màn 3 thì chưa có quảng cáo nào). Không bao giờ khi thua, giữa màn hay khi đang rời màn; app ở
  nền thì bỏ, lần thắng sau hỏi lại. Interstitial chỉ hiện khi rời thẻ thắng, nên thẻ thắng không bao giờ ngay sau quảng
  cáo. Cờ `reviewAsked` chỉ bật khi Play nhận yêu cầu (lỗi mạng thì lần thắng sau hỏi lại) và **giữ qua Xóa dữ liệu**.
  Play tự quyết có hiện hộp hay không và không cho game biết người chơi có đánh giá không, nên không gắn gì vào kết quả;
  không hỏi "bạn có thích không" trước (Play cấm lọc review). Hộp chỉ hiện ở bản cài từ Play (internal track trở lên);
  Editor và APK cài tay không hiện gì. Kỹ thuật: gọi thẳng thư viện `com.google.android.play:review` qua JNI (khai báo
  cho EDM ở `Assets/Scripts/Review/Editor/PlayReviewDependencies.xml`), không dùng plugin Unity của Google vì plugin đó
  kéo EDM bản `com.google.*` trùng với bản `com.unity.*` mà plugin AdMob dùng.

## 14. Kế hoạch sản xuất

Làm theo vai như một đội: **Thiết kế** (tài liệu này, số liệu màn), **Nghệ thuật** (sheet ChatGPT + SVG), **Lập trình**,
**QA** (test tự động + chơi tay trên máy ảo). Mỗi mốc kết thúc bằng: test xanh → review theo khu vực → sửa → build APK → QA
theo checklist → ghi CHANGELOG → commit. Không mốc nào "xong" khi chưa chơi được trên máy ảo.

| Mốc | Nội dung | Định nghĩa xong |
|---|---|---|
| M0 · Duyệt thiết kế | bạn chốt §16 | tài liệu này cập nhật theo quyết định |
| M1 · Nền tảng ngang ✅ | hố bắn, đấu trường, gấu, 10 màn, UI ngang | review 6 khu vực sạch, QA checklist, commit đầu tiên |
| M2–M4 · Theo tham chiếu 🔧 (code xong 11/11, đang test/QA) | gờ chờ, OK!/Grr, bay vòng, lao cắm đầu + nảy + combo, camera lia, đi bộ ra, gấu ăn/ngủ/nằm lại, nhiệm vụ + Perfect, vòng bật, TNT, nút + cổng băng, Ninja, Lửa, màn thưởng cá, boss Vua Gấu Băng + lồng, bản đồ 12 nút, sóng nước, thẻ tải, bảng màu vùng, âm thanh mới | tests xanh, review, QA máy ảo |
| M2 · Cảm giác theo tham chiếu | gờ chờ pha 1, bay vòng lại, lao cắm đầu + nảy + combo, camera lia, đi bộ ra mép, Perfect = hạ hết gấu và nhặt đủ cá, sheet gấu ChatGPT, tiếng | chơi 2 màn trên máy ảo thấy giống nhịp tham chiếu; 3 người thử không cần giải thích |
| M3 · Vật thể mới | cá giấu + Hoàn hảo, vòng băng, cầu lửa, thùng gỗ, màn 5/7 cập nhật | mỗi màn có 3 cá, bot vẫn thắng |
| M4 · Thưởng + Boss | màn thưởng, Vua Gấu Băng, lồng, bản đồ có nút thưởng/boss | thắng boss cứu hải cẩu, 32 sao vùng 1 |
| M5 · Phát hành thử | AAB ký, Play internal testing, icon/ảnh store, chính sách riêng tư | cài từ Play, QA đủ checklist |
| M6+ | vùng 2, vùng 3, thành tích | — |

**Checklist QA mỗi build:** 1 màn thắng + 1 màn thua, chơi lại, tạm dừng/tiếp tục, bản đồ, cửa hàng mua được, âm thanh
bật/tắt, EN/VI, tắt app mở lại giữ tiến trình, xoay hai chiều ngang, 4:3 và 20:9 (Editor), fps ổn trên máy ảo.

## 15. Rủi ro

| Rủi ro | Xử lý |
|---|---|
| Khe thoát quá khó/dễ sau khi đổi số | test quét nhịp (`SwingTimingChangesTheTrajectory`) phải ra cả lọt lẫn không; chơi tay 10 cú |
| Lao chéo chậm làm pha 2 ít "đã" | trọng lực thật sau cú va đầu; rung + mảnh vỡ; chỉnh `diveSpeed` nếu người chơi thử thấy chậm |
| Sheet gấu ChatGPT lệch nét | Chỉnh sửa trên sheet hải cẩu, không tạo mới; so cỡ bằng chiều cao hàng idle |
| make_art ghi đè frame gấu từ sheet | bỏ `bear_*` khỏi `ASSETS` ngay khi slice xong (ghi trong §11) |
| Máy ảo đầy | QA trên `Pixel_QA_34`; không gỡ app dự án khác |
| Phạm vi phình | M1–M5 cố định; mọi ý mới ghi vào §16 chờ duyệt |

## 16. Quyết định đã chốt (người dùng 11/11/2026: "làm hết tất cả để hoàn thiện", theo đề xuất)

1. **Lao:** theo tham chiếu — cắm đầu, rơi nhanh dần, giữ 30% đà ngang, nảy khi trúng gấu, combo ×N. Số trong `Tuning.asset`.
1b. **Đổi hướng trên không:** chạm lần 2 (Ninja): hải cẩu khựng 0.25 s trong bóng thoại rồi lao thẳng xuống. Thay cho vuốt.
2. **Cầu lửa:** kỹ năng mua bằng sao (cấp 1/2/3 = xuyên 2/4/∞ khối băng tới cú va cứng).
3. **Hoàn hảo:** huy hiệu trên bản đồ, không thêm sao. Tổng sao vùng 1 = 36 (12 màn × 3).
4. **Boss:** cuối vùng 1 (màn 12), màn thưởng là màn 11.
5. **Tiền tệ:** chỉ sao.
6. **Tên:** giữ "Pup Launch / Hải Cẩu Bay".
7. Chế độ chiến thuật: không làm. Vùng 2–3: bảng màu (nền + đá) đã có (`LevelData.zone` 0–2, tint trong `AttackPhase.Palettes`); màn mẫu của vùng 2–3 để sau khi vùng 1 qua QA.
