# Báo cáo tham chiếu: Crazy Penguin Catapult (bản web)

Chơi thật 2 lần ngày 12/11/2026 trên Snokido (bản của Digital Chocolate), màn 1–3, quay khung dày (0,3–0,7 s/khung)
để soi hoạt ảnh. Mục đích: học **thể loại, nhịp chơi, bố cục, cách làm phản hồi**. Pup Launch giữ nhân vật, hình,
tên gọi và màn chơi của riêng mình (hải cẩu, gấu bắc cực, Frost Bay); không chép art, tên hay layout màn của CPC.

Ảnh tham chiếu (`Assets/Art/Reference/`):
- `cpc_ref_level_map.png`: bản đồ lúc mới vào.
- `cpc_ref_level_map_progress.png`: bản đồ sau khi thắng màn 1 (sao + nhân vật dời sang ô kế tiếp).
- `cpc_ref_launch_phase.png`, `cpc_ref_launch_phase_wide.png`: pha 1, toàn khung.

Toạ độ trong báo cáo tính theo % khung game (0% = trái/trên, 100% = phải/dưới). Khung gốc 550×500 (gần 1:1).
Pup Launch chạy màn ngang 16:9, nên phải giãn bố cục theo chiều ngang, không copy tỉ lệ.

---

## 1. Luồng màn hình

```
Quảng cáo của cổng game -> Intro có hoạt ảnh -> Màn tiêu đề (Play, loa, nhạc, ?)
  -> Hướng dẫn (lần đầu) -> Bản đồ
  -> Pha 1 (máy bắn) -> Chuyển cảnh "chim nhảy xuống nước"
  -> Panel 4 power-up + thanh Loading -> Continue
  -> Panel nhiệm vụ "Mission N: Hit XX bears" -> Continue
  -> Pha 2 (bổ nhào) -> Panel "Congratulations!" -> Continue
  -> Bản đồ: sao hiện ở ô vừa thắng, nhân vật dời sang ô kế
```

- Mỗi màn có 2 pha. Số chim thoát ở pha 1 = số lượt ở pha 2.
- **Không lưu tiến độ**: tải lại trang thì quay về màn 1 và Total về 0. Pup Launch phải lưu (đã có `SaveData`).
- Phím Space cũng dùng được thay cho click/chạm ở cả hai pha.

## 2. Bản đồ

Xem `cpc_ref_level_map_progress.png`.
- Nhìn từ trên xuống: 2 đảo băng lớn (trái, phải-dưới) và 1 đảo nhỏ (phải-trên), biển xanh có băng trôi.
  Dải trời hoàng hôn với núi xa chạy ngang mép trên.
- Đường mòn khoảng 60 **đá xám tròn có dấu "?"**, uốn chữ S: bắt đầu từ làng igloo góc trên-trái, đi xuống đảo trái,
  vòng sang đảo phải, leo **thang dây** lên đảo nhỏ, kết thúc ở **gấu boss** trên đế cam góc trên-phải.
- Màn đã qua: **sao vàng trên vòng xanh lá**. Màn hiện tại: chim đội mũ lính đứng trên vòng xanh lá và giơ tay chào.
- Năm **lồng** trên đế cam rải dọc đường, trong lồng nhốt chim, có chữ "HELP!". Đây là mốc giải cứu.
- Trang trí: igloo, núi băng, hồ băng, xương cá, cá voi ẩn dưới băng, chữ "pHD", "QHHH" khắc trên tuyết,
  gấu đứng canh ở đảo đầu.
- HUD: 3 nút vuông xanh ngọc (loa, nhạc, ?) ở giữa mép trên. Không có nút back hay pause.
- Hoạt ảnh: sau khi thắng, ô vừa qua đổi sang sao, nhân vật nhảy sang ô kế.

## 3. Pha 1: máy bắn (giải cứu chim)

### 3.1 Bố cục (xem `cpc_ref_launch_phase_wide.png`)

| Vùng | Vị trí | Nội dung |
|---|---|---|
| Trần | trên cùng, dày ~12% | Đá cuội có lõi băng. Mép dưới treo **nhũ băng** ở đoạn 15–55% ngang. Trần dốc lên ở bên phải, mở thành **khe thoát** góc trên-phải |
| Cầu | y ≈ 28%, từ tường trái đến ~55% ngang | Dầm giàn thép/gỗ. **5 chim đội mũ lính** xếp hàng, quay mặt sang phải |
| Dây | buộc ở đầu cầu (~55%), dài ~30% chiều cao | Chim đu, một tay nắm dây |
| Máy bắn | đáy, 40–65% ngang | Khung gỗ, bánh răng xám, 2 bánh xe. Cốc bên trái chứa **cầu tuyết to** |
| Chim vận hành | bục nhỏ bên phải máy (~75%) | Chim đội mũ, tay cầm cần gạt |
| Hố | nửa dưới, hình bát giác | Tường đá cuội viền cát vàng. Khối đá lớn chiếm góc dưới-trái, lõi băng xanh |
| Nền xa | sau hố | Thành phố igloo xanh nhạt, mây hồng, nhạt màu để tách khỏi tiền cảnh |
| HUD | trên | Trái: mặt chim + "x06". Giữa: 4 nút (loa, nhạc, ?, pause) |

### 3.2 Nhịp và luật

- Chạm (hoặc Space) thì **cần máy bật lên**. Cầu tuyết bay **gần thẳng đứng** lên chỗ dây. Có độ trễ nạp ~1 s,
  chạm trong lúc nạp bị bỏ qua.
- **Bắn trúng lúc chim ở điểm thấp nhất** (dây thẳng đứng): chim văng lên-phải qua khe = thoát. Giữa màn hiện
  **chữ "OK" lớn màu cam viền đậm**, mờ dần sau ~1 s.
- **Bắn trúng lúc chim ở biên** (dây lệch): chim văng lệch, đập trần và **nằm kẹt trên nóc trần**, ngửa bụng giãy
  chân. Chim này ở đó đến hết pha (cuối M2 thấy 3 con kẹt thành hàng). Giữa màn hiện **chữ "OOPS" đỏ viền trắng**,
  bán trong suốt.
- **Bắn trượt hẳn**: cầu tuyết trúng chim trên cầu thì có **tia sao vàng** ở chỗ va chạm, **mũ bay khỏi đầu**
  và chim đó ở đầu trần đến hết màn.
- Bộ đếm "x0N" giảm 1 mỗi phát, kể cả phát trượt.
- Chim kế tiếp **đi đến đầu cầu, cúi người, nắm dây và tụt xuống**. Hàng chờ dồn sang phải.
- Mỗi chim mới đu từ gần thẳng đứng rồi biên độ lớn dần. Bắn ngay khi chim vừa xuống dây gần như chắc thoát
  (bấm liên tục mỗi 1,3 s ở M3 vẫn được 5/6). Độ khó nằm ở **biên độ và tốc độ đu**, 3 màn đầu rất dễ.
- Hết chim: **chuyển cảnh** với logo game ở trên, nền xanh dương, mặt nước gợn, **một chim to từ dưới trồi lên**
  giữa màn rồi hạ xuống, như cú nhảy xuống biển. Sau đó là panel power-up kèm thanh "Loading…" sọc đỏ-trắng.

### 3.3 Hoạt ảnh pha 1

| Đối tượng | Chi tiết |
|---|---|
| Chim đu | Một tay nắm dây, thân nghiêng theo dây, chân buông. Đổi chiều mượt kiểu con lắc |
| Chim chờ trên cầu | Đứng thẳng, thỉnh thoảng nghiêng/xoay người. Con bị mất mũ đứng đầu trần |
| Chim lên dây | Đi tới đầu cầu, cúi người, với tay, tụt xuống |
| Máy bắn | Cần quay từ nằm ngang lên đứng, nảy nhẹ cuối hành trình, bánh răng quay. Cốc nạp lại cầu tuyết |
| Chim vận hành | Kéo cần gạt mỗi lần bắn |
| Cầu tuyết | Quả cầu trắng to, bay thẳng lên. Vỡ thành tia/bụi khi trúng |
| Chim thoát | Văng lên-phải, xoay người, ra khỏi khung |
| Chim kẹt | Nằm ngửa trên nóc trần, chân vẫy liên tục |
| Mũ rơi | Bay tách khỏi đầu và rơi xuống |
| Chữ phản hồi | "OK" cam / "OOPS" đỏ, hiện bật lên rồi mờ dần |

## 4. Pha 2: tấn công gấu

### 4.1 Panel nhiệm vụ
Panel xanh nhạt bo góc, tiêu đề "Mission N". Hình: chim đứng cạnh **mũ đặt dưới đất**. Hai dòng chữ:
"Hit XX bears to continue to the next level" và "Total bears: NN". Nút "Continue" ở góc dưới-phải.
Chỉ tiêu: M1 = 2, M2 = 3, M3 = 4 (không tăng đều về sau, xem 10.3).

### 4.2 Bố cục chung

| Vùng | Vị trí | Nội dung |
|---|---|---|
| Trời | 0–70% | Gradient cam đậm (trên) sang vàng (chân trời). Dải mây vằn cam-vàng nằm ngang ở 35–55% |
| Chân trời | ~70% | Núi băng xanh nhạt, mặt biển băng phẳng |
| Đất | 85–100% | Dải đá cuội, mép trên viền **cát vàng**. Dưới đất là băng xanh (thấy khi camera hạ xuống) |
| Tường trái | 0–8% ngang, cao tới ~45% | Vách đá phủ tuyết, dốc xuống vào sân |
| Công trình | giữa | Khối băng xanh trong (vuông + tam giác), có tấm băng **lơ lửng** ở mép phải nhô ra ngoài khung |
| Gấu | trên đất, gò, công trình | 5–7 con mỗi màn |
| HUD trái | trên-trái | Mặt chim + "x0N" (chim còn lại, không tính con đang bay) |
| HUD giữa | trên-giữa | 4 nút: loa, nhạc, ?, pause |
| HUD phải | trên-phải | Vòng tròn mặt gấu cau có. "Total NN" ở trên, "00/04" (đã hạ/chỉ tiêu) ở dưới, số bubble xanh viền trắng |

**Layout từng màn:**
- **M1:** một **gò đá** ở giữa-phải, gấu đấm gió đứng trên đỉnh. Một gấu ngồi cạnh dốc băng tam giác ở bên trái.
  Tấm băng lơ lửng bên phải có gấu ôm bát.
- **M2:** **cổng băng**: 2 cột cao 3 khối, xà ngang 5 khối, tam giác đỡ chân ngoài. 2 gấu trên xà, 1 gấu ngồi
  trong cổng, 2 gấu đứng ngoài, 1 gấu ôm bát trên tấm lơ lửng.
- **M3:** **chữ T bậc thang**: cột băng cao, đỉnh là tam giác. Kệ ngang chạy sang phải rồi hạ một bậc. Gấu trên
  kệ, dưới đất và trên tấm lơ lửng. Bên trái có vùng trũng/dốc thấp hơn, camera chỉ thấy khi đi theo chim xuống.

### 4.3 Điều khiển và vật lý

- Chim bay **từ trái sang phải** ở độ cao ~20%, tư thế **nằm ngang bụng xuống**, mũ hướng về trước, tay ép sau.
  Ra mép phải thì **xuất hiện lại ở mép trái** (wrap), không quay đầu. Qua hết màn hình mất ~2,5 s.
  Chim bay vòng mãi đến khi người chơi chạm.
- Chạm thì **bổ nhào**: thân chúi đầu xuống dần (~45° rồi ~80°), rơi nhanh và trôi ngang ít theo hướng bay.
- Có lúc chạm không ăn. Lần chạm ăn chắc là khi chim ở 1/3 trái màn. Có thể game chỉ nhận chạm trong một vùng ngang
  (chưa xác định chắc).
- **Camera đi theo chim**: khi bổ nhào, camera hạ xuống thấy cả băng dưới đất và trượt ngang theo chim. Sân rộng
  hơn khung một chút, nên lúc chim bay camera cũng trôi ngang nhẹ.

### 4.4 Va chạm và hoạt ảnh pha 2

| Sự kiện | Hoạt ảnh |
|---|---|
| Trúng khối băng | Khối **vỡ thành mảnh vuông nhỏ** xanh bắn toả kèm tia sáng. Mảnh rơi theo trọng lực. Khối bên trên mất đỡ thì rơi theo |
| Trúng gấu | Gấu **lật ngửa**, miệng há, 4 chân chổng lên, trượt/rơi khỏi chỗ đứng, thường rơi xuống nước. HUD +1 ngay, cả Total |
| Mũ chim | **Bay khỏi đầu** khi va chạm mạnh, rơi riêng |
| Chim sau va chạm | Nảy lên, lộn vòng (có thể hạ thêm gấu: M2 và M3 đều hạ 3 gấu bằng 1 chim), lăn với **cụm bụi trắng tròn**, ngồi choáng rồi biến mất |
| Gấu đứng trên khối bị phá | Rơi theo khối, vẫn được tính |
| Hết chim hoặc đủ thì chờ | Panel kết quả bật lên khi con cuối dừng hẳn. Không kết thúc sớm dù đã đủ chỉ tiêu |

### 4.5 Gấu: hành động đứng yên (mỗi con một kiểu, lặp ~1–2 s)

- **Đấm gió**: đứng, hai nắm đấm trước ngực, tay thụt ra thụt vào.
- **Ôm bát**: cầm bát xanh lá, cúi ăn, ngẩng lên nhai.
- **Ngồi ôm gối**: ngồi bệt, đầu xoay qua lại.
- **Khoanh tay vênh mặt**: đứng, mắt nhắm, cằm hếch.
- **Bị trúng**: lật ngửa, mắt xoáy, chân chổng.
- Gấu không đi lại và không phản công. Chỉ có hoạt ảnh tại chỗ.

### 4.6 Kết thúc

- **Thắng**: panel "Mission N / Congratulations!". Hình: chim đứng trên gấu nằm ngửa mắt xoáy. Dòng "Bears hit:
  05/03" (được vượt chỉ tiêu), "Total bears: 08", nút "Continue". Khi panel mở, nút pause ẩn khỏi HUD.
- **Thua**: **không ép ra được** ở M1–3. Đã cố bắn sai thời điểm ở pha 1 (vẫn thoát 5/6) và bổ nhào sớm ở pha 2
  (vẫn nảy chuỗi trúng 3 gấu). Ba màn đầu là màn dạy chơi, gần như không thể thua. FrostFling thì thua được ở màn 3 và thấy panel Game Over (10.1).

## 5. Pause

Nút pause (II) phủ lớp mờ xám, chữ "PAUSE" trắng ở giữa, mọi thứ đứng hình. Bấm lại nút để chơi tiếp. Không có menu
(không có Restart/Map trong lúc pause).

## 6. Phong cách hình ảnh

- 2D vẽ tay kiểu cartoon, viền nâu/đen dày, tô phẳng, shading 2 tông.
- Bảng màu: **pha 1 lạnh** (xanh băng, đá xám, mây hồng) và **pha 2 ấm** (cam hoàng hôn, băng xanh tương phản).
- Vật liệu: đá cuội tròn xám-nâu, viền cát vàng. Băng xanh trong có vân sáng. Nhũ băng trắng.
- UI: panel xanh nhạt bo góc lớn, viền xanh đậm dày. Chữ trắng viền xanh kiểu bubble. Nút viên thuốc xanh nhạt.
  Nút HUD vuông bo góc xanh ngọc với icon trắng.
- Chữ phản hồi giữa màn (OK/OOPS): to, nghiêng nhẹ, viền dày, màu khác nhau cho đúng/sai.

## 7. Âm thanh

Có nút loa (SFX) và nhạc riêng ở mọi màn. Hai phiên chơi đều không nghe được tiếng nên chưa có dữ liệu.

## 8. Rút ra cho Pup Launch (giữ nhân vật riêng)

| CPC | Áp dụng cho Pup Launch |
|---|---|
| Bắn ở điểm thấp nhất = thoát, ở biên = kẹt | Cửa sổ thời điểm rõ ràng, đọc được bằng mắt |
| "OK" cam / "OOPS" đỏ giữa màn | Chữ phản hồi đi kèm **hình** (con kẹt, mũ rơi). Màu không là tín hiệu duy nhất |
| Con kẹt ở lại trên trần đến hết pha | Đếm thất bại bằng hình ảnh |
| Chuyển cảnh nhảy xuống nước giữa 2 pha | Cảnh nối 1–2 s, che lúc tải pha 2 |
| Bay ngang và wrap từ phải sang trái, chạm để bổ nhào | Khớp GDD v8. Nhớ cho chạm ăn ở mọi vị trí trên màn hình (CPC có lúc nuốt chạm, gây khó chịu) |
| Camera theo con đang bổ nhào | Camera ngang + hạ nhẹ khi bổ nhào |
| Băng vỡ thành mảnh, khối trên sập theo | Nguồn "wow" chính: một con hạ được nhiều gấu |
| Gấu đứng yên, mỗi con một idle | Đã chốt 11/11 |
| Chỉ tiêu tăng dần nhưng không đều (2,3,4,4,4,5,6,5,5, boss 1), được vượt, Total cộng dồn | Total dùng cho bản đồ/boss |
| 3 màn đầu không thể thua | Màn 1–3 làm tutorial, độ khó bắt đầu từ màn 4 |
| Không lưu tiến độ | Pup Launch **phải** lưu (kill + reopen giữ tiến độ, theo checklist QA) |

## 9. Phần bổ sung (chơi lần 3 + xem 3 video YouTube, 12/11/2026)

Nguồn video: "Crazy Penguin Catapult (Browser/Flash) - Gameplay | No Commentary" (13:39, tới màn 11),
"Crazy Penguin Catapult (2008) - Flash Version" (23:07, tới màn ~20), "Dave's Barrel of Flash Games Ep. 6" (32:37, tới màn 17).

### 9.1 Intro (trước màn tiêu đề, ~25 s, có nút "Skip" dưới-phải)
1. Logo nhà phát hành trên nền trắng.
2. Cảnh băng ngày nắng: làng igloo xa, đàn chim đứng vui trên băng.
3. Ba gấu (bụng có vệt màu xanh, hồng, vàng để phân biệt) đứng khoanh tay. Một **quả cầu tuyết lăn tới**.
4. Cầu tuyết lăn qua **cuốn chim vào trong** (chim dính quanh quả cầu), lăn về phía **lồng sắt** mở cửa.
5. Gấu cười đắc ý, một chim bay thoát qua mép trên.
6. Gấu **xách lồng nhốt chim** bỏ đi.
Đây là lý do có lồng trên bản đồ: chim bị gấu bắt, người chơi đi giải cứu.

### 9.2 Màn tiêu đề
Logo "Crazy Penguin Catapult" (chữ "Crazy" xanh lá uốn lượn, "Catapult" chữ đá-gỗ cam). Nền hoàng hôn cam,
biển băng. Nút "Play" xanh to có icon, nút "Play More Games" xanh lá nhỏ hơn. 3 nút loa, nhạc, ? ở trên-giữa.

### 9.3 Combo và chữ phản hồi
- Pha 1: "OK" cam (thoát), "OOPS" đỏ (kẹt), và **"FIRE!" đỏ-cam viền vàng** to giữa màn, thấy ở màn 17. Có vẻ đó là
  lúc nhận power-up lửa (xem 9.4).
- Pha 2: một chim hạ nhiều gấu thì hiện **"x2", "x3"** bằng số nâu-vàng to, nổi lên từ chỗ va chạm. Gấu bị trúng
  bay lên với tia sao.
- Panel kết quả đạt tối đa: tiêu đề phụ **"Perfect!"** vàng có sao lấp lánh, thay cho "Congratulations!". Hình là
  **3–4 chim nhảy múa quanh gấu nằm ngửa**. Hạ vượt chỉ tiêu nhiều (vd 10/06) vẫn hiện "Perfect!".

### 9.4 Power-up
- Ở màn 17 trở đi, pha 1 có một **vật bay tròn (huy hiệu/đĩa sáng) lơ lửng ở góc trên-phải**, cạnh khe thoát.
  Chim thoát bay trúng nó, hoặc ngay khi bắn, thì hiện "FIRE!" và có tia lửa vàng toả ra.
- Ở pha 2 sau đó, chim bổ nhào có **vệt lửa vàng-cam** và phá băng hàng loạt (vỡ thành mảnh nhỏ bay rất xa).
  Một chim hạ được 4–6 gấu.
- Theo mô tả trên store, power-up (Ninja, Fire, Twins, Rubber) **mở khoá sau khi thắng boss**. Twins = bắn ra 2–4
  chim, Rubber = nảy thêm 2 lần, Fire = xuyên băng, Ninja chưa rõ. Mô tả này chưa kiểm chứng trên bản web.

### 9.5 Màn boss (màn 10, ô lồng đầu tiên)
- Panel nhiệm vụ đổi thành **"Hit the Boss bear several times to knock it out!"**, hiện dạng băng chữ ở đáy màn.
- Chỉ tiêu "00/01". HUD vẫn đếm chim.
- Bố cục:
  - Trái: **lồng sắt treo** nhốt một chim, trong một hốc đá có viền băng.
  - Giữa: hai gò đá cuội dốc chéo tạo hình chữ V, có **nút bấm đỏ/xanh** ở đáy hốc.
  - Phải: **gấu boss** đứng trên tảng băng, giữa 2 kệ **thùng gỗ nâu** (vật liệu mới).
- Boss có HP: bị trúng thì **chao đảo, hiện sao xoay quanh đầu** nhưng không ngã ngay. Phải trúng nhiều lần.
- Khi boss gục: panel "Mission 10 / Perfect!", hình chim nhảy múa quanh boss nằm ngửa. Lồng mở, chim được thả.
- Trên bản đồ: lồng ở mốc đó **mở cửa, trống**. Đường đi tiếp sang đá xám kế.
- Nút đỏ có thể là công tắc mở lồng. Chưa thấy rõ chim trúng nút thì chuyện gì xảy ra.

### 9.6 Độ khó tăng dần (màn 4–20)
- **Pha 1**: cùng một bố cục (không đổi cảnh). Thay đổi là dây đu nhanh hơn, biên độ lớn hơn và số chim nhiều hơn
  (màn 16–17: 7–9 chim trên cầu). Chim kẹt trên trần thành hàng dài.
- **Pha 2**: chỉ tiêu tăng (M8 = 5, M11 = 4, M16 = 6, M20 = 7). Bố cục đa dạng hơn:
  - Hang ngầm: hố đá cuội hình bát giác, trời cam chỉ thấy qua lỗ, khối băng ở đáy (màn ~4).
  - Gò dốc + tấm băng ngang dài nối 2 trụ (màn 5–6).
  - Tháp băng chữ A / kim tự tháp có gấu ở mọi tầng (màn 7–8).
  - Băng nổi lơ lửng nhiều tầng, gấu đứng trên từng tầng (màn 16).
  - Gấu **đội mũ lính xanh** (lấy được mũ chim?) xuất hiện từ màn ~11.
  - Thùng gỗ xuất hiện từ màn boss.
- Panel nhiệm vụ đổi hình minh hoạ: chim đứng cạnh mũ (màn thường), chim đội mũ lính (màn 8).

### 9.7 Màn thua
- **Vẫn không ép ra được**, kể cả khi cố tình. Ở màn 1, bổ nhào vào chỗ trống thì chim vẫn trượt dọc mặt đất,
  lăn vào đám gấu và hạ 2/2. Cả 3 video cũng không có lần thua nào.
- **Chim chưa bổ nhào bay vòng mãi**, không giới hạn thời gian (đã chờ 20 s). Màn chỉ kết thúc khi chạm.
- Kết luận: bản Flash rất dễ. Vật lý nảy/trượt rộng tay nên gần như luôn đủ chỉ tiêu. Pup Launch cần tự thiết kế
  màn thua (gợi ý: panel "Try again" + nút Retry/Map) và **siết vật lý** ở màn sau để thua là có thể xảy ra.

### 9.8 Bản đồ thêm
- Vòng màn đã qua: **vòng xanh lá có sao vàng**. Màn đang chọn có viền xanh lá sáng.
- Nhân vật trên bản đồ là **chim đội mũ lính** đứng trên ô hiện tại.
- Lồng đã giải cứu: mở cửa, trống. Lồng chưa: chim bên trong, đế cam.

### 9.9 Màn thua (bản iOS cùng series, video "Mission Failed/Game Over (Boss Levels)")
- Hết chim mà chưa đủ chỉ tiêu: panel **"MISSION FAILED!"** chữ xám. Bên dưới là icon mặt gấu + số gấu đã hạ và
  **3 ngôi sao rỗng**. Hai nút: **"OK"** (trái) và **"More"** (phải).
- "More" mở panel **"STATISTICS"**: Bears hit total, 2x bears hit, 3x bears hit, 4x or more bears hit (đếm số lần
  hạ combo). Nút OK / Back.
- Sau OK: **cảnh phim ngắn trên nền xanh trơn**, gấu boss xách **lồng có chim** bỏ đi (chim vẫn bị nhốt). Có thanh
  tiến độ mảnh ở đáy. Rồi về bản đồ, ô màn đó vẫn chưa qua.
- Bản iOS có **sao 0–3 mỗi màn** và đếm tổng sao trên bản đồ ("27/30"). Bản web chỉ có sao vàng = đã qua.
- Ở bản web không ép thua được (xem 9.7), nên panel thua của bản web vẫn chưa thấy tận mắt.

### 9.10 Nút trong màn boss (bản web, màn 10, xem từng giây)
1. Bắt đầu: nút ở đáy hốc chữ V màu **xanh lá** (nổi lên). Boss đứng trên tảng băng sát kệ thùng gỗ.
2. Chim 1 bổ nhào vào hốc, **đáp lên nút**: nút đổi sang **đỏ** (bị ấn). Chim ngồi choáng cạnh nút, cụm bụi trắng.
3. Boss sau đó đứng ở mép băng, trên kệ thùng gỗ thấp hơn (vị trí đổi so với lúc đầu).
4. Chim 2 bay ngang, bổ nhào trúng boss: boss rơi khỏi khung, "01/01", hiện "Perfect!".
5. Lồng mở, trên bản đồ lồng trống.
- Kết luận: nút là **công tắc**, ấn một lần thì đổi trạng thái màn chơi (dời/hạ chỗ boss đứng, làm boss dễ trúng).
  Chưa thấy rõ cơ chế bên trong (băng tan hay bục hạ).

### 9.11 "FIRE!" và vật bay góc phải (xem lại từng giây, màn 17)
- Vật tròn sáng (đĩa ánh xanh-vàng) **có sẵn ở góc trên-phải từ đầu pha 1** ở màn 17, cạnh khe thoát.
- Chữ "FIRE!" hiện **ngay lúc máy bắn**, khi chim đang ở **điểm thấp nhất** của cú đu (đúng thời điểm tốt).
  Nó xuất hiện ở nhiều phát trong cùng màn, không chỉ một lần.
- Ở phát cuối, đĩa vỡ thành tia cam: có thể chim thoát đã bay trúng đĩa.
- Cách hiểu hợp lý nhất: "FIRE!" là **lời khen khi bắn đúng thời điểm vàng** (bậc cao hơn "OK"). Đĩa là **mục
  tiêu thưởng** để chim thoát bay trúng. **Chưa kiểm chứng được** đĩa cho phần thưởng gì: video không có tiếng
  và không có chú thích.

## 10. Bổ sung từ báo cáo repo FrostFling (12/11/2026)

Nguồn: `~/Documents/MyProject/new-game/FrostFling/Docs/BAO-CAO-PHAN-TICH-GAME.md`. Báo cáo đó chơi bản Flash trên
flashmuseum.net (menu ghi `Version 1.0.3`, © 2008). Họ thua màn 3 hai lần và xem từng khung 0,5 s đoạn boss của video
Flash `1Dp_WM-9ag0`. Ký hiệu: **[FF]** = lấy từ báo cáo FrostFling, mình chưa tự thấy. **[FF+PL]** = cả hai báo cáo
cùng thấy.

### 10.1 Màn thua (Game Over) bản web [FF]
Phần này bù cho chỗ mình chưa thấy ở 9.7.
- Pha 1 trượt nhiều thì thiếu chim, hết chim mà chưa đủ gấu là thua. FrostFling thua màn 3 vì chỉ 2 chim qua khe, trong
  khi cần hạ 4 gấu. Màn 1–2 thì khó thua, khớp với 9.7.
- Panel **"Game Over ...... Or is it ?"**: hình 2 gấu cười đắc thắng, một chim nằm bẹp ở giữa.
- Nút **Play More Games** (xanh lá, nổi bật) và nút **Play Again** (nhỏ hơn).
- Play Again đưa về **Main Menu**. Bản đồ **reset về ô đầu**: không có checkpoint, không lưu.
- Pup Launch làm khác: thua chỉ chơi lại màn đó (đã có ở GDD §R5), không reset bản đồ.

### 10.2 Mức đánh giá cuối màn [FF+PL]
- **"Perfect!"** = hạ **hết** gấu trong màn. Hình: 4 chim đứng quanh gấu nằm.
- **"Congratulations!"** = đủ chỉ tiêu nhưng chưa hạ hết. Hình: 1 chim đứng trên 1 gấu nằm. Ví dụ M7 09/06, M8 08/05.
- **Hạ hết gấu thì màn kết thúc sớm** và hiện "Perfect!" ngay, dù còn chim. Chim thừa không cộng điểm.
  Chỉ đủ chỉ tiêu thì vẫn chơi tiếp tới chim cuối (khớp 4.6).
- Pup Launch đã làm đúng như vậy: "Nhiệm vụ hoàn hảo!" và "Hoàn thành!", hạ hết gấu thì kết thúc sau `WinDelay` 1,6 s.

### 10.3 Chỉ tiêu gấu màn 1–10 [FF]
M1 = 2, M2 = 3, M3 = 4, M4 = 4, M5 = 4, M6 = 5, M7 = 6, M8 = 5, M9 = 5, M10 (boss) = 1.
**Không tăng đều +1** như mình ghi ở 4.1 (mình chỉ thấy M1–3). Video của mình cho thêm: M11 = 4, M16 = 6, M20 = 7.

### 10.4 Pha 1: bố cục và nhịp, chỗ hai báo cáo khác nhau
- **[FF]** thấy 3 kiểu bố trí pha 1, đổi ngẫu nhiên giữa các lượt:
  - A: dây đu con lắc.
  - B: dây bungee dọc, một "chim chỉ huy" trên cầu kéo dây lên xuống.
  - C: dây đu + máy bắn, **2 lần chạm**. Chạm 1 thả chim rơi vào gầu máy. Chạm 2 bắn. Thả lệch thì chim rơi đất, mất lượt.
- **Mình (PL)** chơi 3 lần trên Snokido, cả 3 lần đều cùng một bố cục và **1 lần chạm**: máy bắn quả cầu tuyết lên
  trúng chim đang đu (mục 3). Chưa lần nào thấy kiểu bungee hay kiểu 2 chạm.
- Kết luận: có thể là hai phiên bản Flash khác nhau (flashmuseum 1.0.3 và Snokido), hoặc FrostFling hiểu nhầm cảnh cầu
  tuyết bay lên. **Pup Launch giữ kiểu 1 chạm** đã chơi tận mắt. Kiểu bungee để làm biến thể pha 1 sau này (ý tưởng
  cho vùng 2).
- **Phát trượt [FF]**: có thêm cảnh **cận mặt chim phóng to, ép bẹp vào "kính" màn hình**, rồi trượt xuống sau màn
  nước. Mình chỉ thấy chim kẹt trên trần (3.2). Đây có thể là cảnh của kiểu B. Pup Launch dùng "kẹt trong nhũ băng /
  nằm ngửa trên sàn" (GDD §R3).
- **[FF]** Bản Java có mức **"Great!" xanh lá**, cao hơn "OK". Pup Launch đã có "TUYỆT!".
- **[FF]** Chim trên cầu chờ: đứng lắc lư, giơ cánh vẫy, quay đầu nhìn. Con kế tiếp tự bước lên cầm dây.

### 10.5 Pha 2: chi tiết thêm [FF]
- **Gấu bị hạ biến mất** khỏi màn sau vài khung nằm ngửa. Pup Launch đang để gấu nằm lại tới hết màn (GDD §0): giữ
  vậy, vì người chơi đếm được số gấu đã hạ bằng mắt.
- Mỗi gấu bị hạ bắn ra **1 ngôi sao vàng bay vào bộ đếm** ở góc phải HUD. Pup Launch chưa có: đề xuất thêm.
- Combo đã thấy **x2 và x4**. Chữ combo màu xám, nghiêng, to, đặt cạnh gấu (mình thấy màu nâu-vàng ở giữa màn).
- Chim có thể **đứng trên bụng gấu nằm** giơ cánh ăn mừng.
- Chim trúng khối băng vỡ ra 5–10 mảnh vuông. Chim lăn tròn thành quả bóng xuống dốc. Chim trượt bụng có bụi tuyết.
- **Ninja [FF, bản Java]**: sau khi chim đáp, hiện **bong bóng tròn viền cam** có hình chim ninja, màn chờ khoảng 3 s.
  Chạm trong lúc đó thì chim bật nhảy đá chéo sang gấu kế bên, trúng là hạ thêm 1 gấu (thấy combo x2). Bản mình
  làm: chạm lần 2 khi đang lao thì khựng rồi rơi thẳng (GDD §16). Có thể làm thêm "đá chéo sang gấu gần nhất".

### 10.6 Màn boss bản Flash (màn 10), xem từng khung 0,5 s [FF]
Bổ sung và sửa mục 9.5, 9.10 của mình.
1. **Có panel nhiệm vụ**: "Mission 10 – Hit 01 bear…", hình chim lính đội mũ xanh, lồng thấy ở góc trái dưới.
2. Băng chữ đáy màn: "Hit the Boss bear several times to knock it out!".
3. Bố cục: khoang lục giác bên trái treo **lồng đồng** có 1 chim, hố chữ V ở giữa có **nút xanh lá trên bệ đá**, gấu
   boss bên phải đứng trên **cầu thùng gỗ**, khoanh tay, có **làn hơi xoắn trên đầu** (đang giận). Boss không to hơn gấu
   thường, không có thanh máu.
4. **Cú trúng 1**: boss hiện 4 sao choáng, mũ chim văng ra. Boss lùi lại nhưng chưa thua (`00/01`).
5. Chim nảy rơi vào hố, **đáp lên nút: nút xanh → đỏ**, bụi tuyết bung ra. Sau đó **cầu thùng gỗ phía trên boss
   không còn** (khớp 9.10: chỗ boss đứng đổi).
6. **Cú trúng 2**: boss biến mất, `01/01`. **Boss 1 cần 2 cú trúng.**
7. **Cảnh giải cứu (~1,5 s)**: camera lia sang lồng, **lồng nổ tung**: chấn song bung cong ra, nhiều sao vàng xoay
   quanh. Chim bị nhốt bật lên, đáp xuống đất, giơ hai tay vẫy.
8. Panel "Mission 10 – Perfect!". Trên bản đồ, **ô lồng biến mất**, avatar đứng tại chỗ lồng cũ.
- Pup Launch: Vua Gấu Băng (màn 12) đang dùng 3 lần trúng qua nút (GDD §16, `BossBear`). Nên thêm hiệu ứng **lồng nổ
  tung có sao + hải cẩu đáp đất vẫy tay** cho `Cage.Open`.

### 10.7 Màn hình phụ bản web [FF]
- **Intro**: logo nhà phát hành ở góc dưới trái, có trên **mọi** màn hình.
- **Main Menu**: hàng 3 icon nhỏ trên-giữa (Sound, Music, ?). Logo 2 dòng: chữ viết tay xanh lá + chữ gỗ cam nổi khối.
  Nút Play to (viền xanh lá dày, nền trắng kem, icon chim đỏ bên trái). Nút phụ Play More Games. Bản quyền và version
  ở góc dưới phải. Nền: hoàng hôn, mây sọc trôi ngang chậm (parallax), biển băng có băng trôi.
- **Instructions**: panel 2 cột. Cột "Catapult": hình máy bắn + icon chuột/phím SPACE, chữ "Left click the mouse or
  press space bar to fire the catapult." Cột "Diving": chim bay + gấu + icon chuột/SPACE, chữ "…to dive." Nút
  Continue góc dưới phải. Panel tự hiện khi bấm Play ở đầu mỗi lượt.
- **Panel power-up giữa 2 pha**: tiêu đề "Check out all 4 … power-ups!". 4 dòng có icon: Twins (2 chim), Rubber
  (đường nảy), On Fire (vệt lửa), Ninja (tư thế đá võ). Có nút cross-promo và nút Continue.
- **Pause**: màn mờ, chữ "PAUSE", có nút cross-promo bên dưới.
- **Bản đồ**: khoảng 50–60 ô, **5 lồng** cách nhau 7–10 ô (lồng đầu ở **màn 10**), **2 gấu đứng** (một ở giữa bản đồ,
  một là boss cuối trên đảo cuối). Ô lồng **không bấm vào được**, phải đi lần lượt từng ô. Lồng đầu có chữ đỏ "HELP!"
  và icon búa nhấp nháy.

### 10.8 Bản Java / iOS (cùng series, khác bản web) [FF]
Tham khảo cho tính năng, không phải bản đã chơi.
- **Menu**: Play Campaign, Play Strategy Game, Instructions, High Scores, Settings, About.
- **Intro**: 3 gấu boss bụng xanh, hồng, vàng. Boss hồng nhốt chim vào lồng.
- **Bản đồ dọc nhiều vùng**: đảo băng, ốc đảo có cây dừa, núi lửa. Bộ đếm sao `03/36`. Dòng gợi ý "2 more stars to
  earn an upgrade!".
- **Star Upgrade**: đủ sao thì chọn 1 nâng cấp vĩnh viễn: Rubber Penguin ("Two more bounces"), **Ice Breaker**,
  Penguin on Fire, **Double Ninja** ("Move like a ninja twice"). Pup Launch dùng cửa hàng sao, cùng ý.
- **Boss Java**: đấu trường kín nhìn hết trong một màn, nhiều tầng. Lồng treo, nút đỏ/xanh. Boss di chuyển giữa các bệ.
  3 đấu trường:
  - boss vàng: pháo đài đá xám, cầu thùng gỗ;
  - boss xanh: tháp băng kiểu chùa có rương đỏ, sập từng phần;
  - boss hồng: hang đá cam có nhũ đá nhọn.
  Trước boss có **11 chim** (màn thường 6), biển số đếm ngược dưới cầu.
- **Màn loading Java**: gấu bụng hồng nhảy đuổi 3 chim đang run, có thanh tiến độ. Pup Launch có cảnh gấu xách lồng
  chạy ngang, cùng ý.
- **Màn thưởng**: chim chạy nhặt kẹo, đếm `48/25`; thả nhiều chim xuống dốc (`24/12`).
- **Vật cản**: khối đinh / bánh răng cam nguy hiểm, trần nhũ đá, hang dung nham.
- **Vùng**: băng xanh, đá xám tuyết rơi, đá đỏ dung nham gần núi lửa.
- **Kết thúc**: cảnh chim bay húc 2 gấu boss, có vệt sao vàng.
- **Strategy mode**: bàn cờ kiểu Risk, tấn công ô kề bên, thắng nếu số chim nhiều hơn số gấu ở ô đó. Pup Launch
  không làm (GDD §16).

### 10.9 Bảng màu ước lượng [FF]
Trời cam `#F07A1A` → vàng `#FFD24A`. Băng `#5AD2F0` / `#2FA9E0`. Viền đất vàng của đá `#D9A441`. Panel UI
`#DCEFFB`. Nút xanh ngọc `#1F7F8C`. Viền nút chính xanh lá `#6DBE3F`. Pup Launch dùng bảng màu riêng (GDD §11, tranh
ChatGPT), chỉ tham khảo cảm giác ấm/lạnh.

### 10.10 Việc rút ra cho Pup Launch (đã làm hết, 12/11/2026)
| Từ quan sát | Đã làm trong Pup Launch |
|---|---|
| Sao bay từ gấu vào bộ đếm HUD | `HudView.StarToCounter`: mỗi gấu bị hạ bắn 1 sao vàng bay vòng cung vào huy hiệu gấu, huy hiệu nảy khi sao tới ✅ |
| Lồng nổ tung có sao, chim vẫy tay | `Cage.Open`: lồng phình và rung, 8 chấn song bay ra, 5 sao xoay quanh, hải cẩu cuộn tròn bật ra, đáp sàn nén-giãn rồi vẫy tay (`cheer`) ✅ |
| Hải cẩu đứng trên bụng gấu ăn mừng | `PufferController.WalkOff`: hải cẩu dừng trên gấu đã gục thì đứng lên bụng gấu, tung sao, ăn mừng 1,1 s rồi mới đi ✅ |
| Ninja đá chéo sang gấu kế bên | `NinjaDrop`: sau lúc khựng, lao chéo vào gấu đứng gần nhất phía dưới (trong 5 đơn vị ngang); không có gấu thì rơi thẳng ✅ |
| Boss có làn hơi giận trên đầu | `BossBear.Update`: hơi trắng phụt luân phiên từ hai tai khi Vua Gấu đứng trên bệ ✅ |
| Kiểu pha 1 bungee | `LaunchStyle.Bungee` (`SwingRope.Bungee`): dây thẳng đứng co giãn, hải cẩu nhún lên xuống ngay trên máy bắn; "TUYỆT!" khi dây giãn hết. Dùng ở màn 5 và 8, có băng chữ nhắc lúc vào màn. Đường ray ngắm thấp hơn (40°, kéo 0,7) vì không có đà đu ngang ✅ |

## 11. Chơi lại bản flashmuseum (12/11/2026, chiều)

Bản `flashmuseum.net` (Ruffle). Đây là bản FrostFling đã chơi. Mình chơi màn 1–3, quay khung dày khoảng 0,3 s. Ảnh:
`Assets/Art/Reference/cpc_ref_map_flashmuseum.png`, `cpc_ref_pit_flashmuseum.png`.

### 11.1 Kết luận các chỗ còn mâu thuẫn
- **Pha 1 là 1 lần chạm.** Màn hướng dẫn trong game ghi: chạm để bắn máy, chạm để bổ nhào. 3 màn đều cùng một bố cục
  (dây đu + máy bắn ở đáy). Không thấy kiểu bungee hay kiểu 2 chạm. Kiểu bungee của Pup Launch là biến thể riêng.
- **Thua rất khó**: cả 3 màn đều thắng "Perfect!" dù cố bắn sai nhịp. Một chim nảy chuỗi hạ 2–3 gấu.

### 11.2 Chi tiết hoạt ảnh mới thấy
**Pha 1:**
- Chim đi tới đầu cầu rồi **nhảy xuống ôm dây**. Dây đang đu thì hất chim lên. Con kế **chờ ở mép cầu, cúi nhìn xuống**.
- Hàng chờ chỉ **3–5 con hiện trên cầu**. Con mới nối vào từ mép trái màn hình.
- Chim kẹt trần **nằm ngửa trong nhũ băng, cánh và chân đạp**. Nhiều con kẹt thì nằm cạnh nhau.
- **Chim trúng cầu tuyết lúc đang trên cầu** (chưa ôm dây) thì bị văng khỏi cầu, rơi xuống, trúng đá và nằm sấp ở gờ trái.
  Con này cũng bị tính là mất.
- Máy bắn: **cần quay lên, quả cầu tuyết bay theo cung tròn**. Quả cầu nằm trong cốc và lăn xuống đáy hố khi bắn trượt.
- Trong lúc máy nạp lại, chạm bị bỏ qua (không có hàng đợi lệnh).

**Pha 2:**
- Bay ngang: chim nằm thẳng như ngư lôi, **cánh dang ra sau, thân rung nhẹ**. Mất khoảng 3 s qua hết màn.
- Bổ nhào: thân **xoay dần từ nằm ngang sang chúi 70–80°** trong khoảng 0,3 s. Có **vệt gió trắng sau đuôi**.
  Rơi gần thẳng xuống.
- **Va chạm:**
  - Trúng gấu: gấu **bay lên trời xoay vòng kèm sao và mảnh băng**, rồi rơi khỏi màn. **Gấu bị hạ biến mất.**
  - Trúng đất: chim **nảy và lộn**, có **cụm bụi trắng tròn to**.
  - Chim choáng nằm có **2–3 quả cầu tuyết nhỏ xoay quanh đầu** (thay cho sao).
  - Chim đứng dậy, rồi **nảy bật lên bay tiếp sang phải**, có thể hạ thêm gấu.
- **Mặt đất có hố**: một số màn có **hang ngầm bên dưới** (đường hầm đá cam, nhũ băng). Chim rơi qua khe thì **camera
  đi xuống theo**. Bên dưới có thêm gấu và khối băng.
- **Khối băng vỡ thành 10–15 mảnh vuông nhỏ** bay toả theo hướng va. Dầm băng mất đỡ thì sập.
- **Gấu idle đã thấy:** ôm bát cá xanh; vươn tay chỉ về phía chim kèm "ha ha"; vỗ bụng; đứng ngó quanh; **ngồi trên dầm
  băng, chân thõng**.

**Màn hình và giao diện:**
- Bản đồ: chim lính đứng trên ô hiện tại, ô đã qua là **sao vàng trên đế xanh lá**, ô kế chưa qua nhấp nháy.
- Thẻ nhiệm vụ: chim lính **chào tay** đứng trên mai rùa xanh.
- Thẻ Perfect: 4 chim đứng quanh gấu nằm. Chim thì **nhảy, vẫy cánh**, gấu nằm ngửa thè lưỡi.
- HUD đấu trường: bộ đếm `00/02` **nảy to lên rồi về** mỗi khi hạ gấu.

### 11.3 So với Pup Launch hiện tại: đã cải thiện (12/11/2026)
Đã làm hết 6 dòng dưới đây:
- gấu văng xoay (`Bear.SpinUp`);
- hang ngầm màn 6 và 9 (`LevelData.caveFrom/caveTo/gapFrom/gapTo`, `AttackPhase.BuildFloor`, camera xuống thêm 1,6);
- cầu tuyết choáng (`Fx.DizzyOrbit`);
- vệt gió (`Fx.WindStreak`);
- gấu ngồi thõng chân (`bear_sheet_e.png` cột 3, `BearMood.Dangle`);
- hải cẩu chào tay trên thẻ nhiệm vụ (`pup_sheet_g.png`, `FrameLoop`).


| CPC | Pup Launch | Đề xuất |
|---|---|---|
| Gấu trúng bay lên xoay vòng rồi biến mất | gấu ngã ngửa, nằm lại | thêm cú **văng lên xoay vòng** trước khi nằm (giữ nằm lại để đếm) |
| Hang ngầm bên dưới đấu trường, camera theo xuống | đất liền | màn 6–10: thêm **khe đất + hang dưới** có gấu |
| Chim choáng có cầu tuyết xoay quanh đầu | sao | đổi sao thành **3 quả tuyết nhỏ xoay** |
| Chim rơi khỏi cầu khi trúng cầu tuyết | chỉ giật mình | giữ giật mình (đỡ phạt người mới) |
| Hàng chờ nối dài ra ngoài màn | cầu chứa 8 con | đủ dùng, không đổi |
| Vệt gió trắng khi bổ nhào | vệt bụi | thêm **vệt gió thẳng** (2–3 đường trắng) |
| Gấu ngồi thõng chân trên dầm | ngồi ôm gối | thêm biến thể ngồi thõng chân (cần vẽ thêm) |
| Thẻ nhiệm vụ có chim chào tay | thẻ nhiệm vụ có hình hải cẩu đứng | dùng frame cheer hoặc vẽ thêm frame chào |

## 12. Xem video Flash từng 0,06 s (12/11/2026, tối)

Video `1Dp_WM-9ag0`, đoạn 17,3–21,7 s (pha 1) và 38–42 s (pha 2). Phần này **sửa** các mục 3.2 và 11.2 cũ.

**Pha 1, cơ chế thật:**
- Quả cầu tuyết **gắn cố định ở đầu cần** máy bắn. Lúc nghỉ, nó nằm ở phía dưới-trái máy (trông như đối trọng).
- Chạm thì cần quay lên qua đỉnh trong khoảng 0,1 s. Chính **quả cầu quét cung tròn và đập vào chim** (có chớp sáng
  vàng chỗ va). Cầu không bay đi.
- Chim bị đập lúc đang ở **nửa phải cung đu**, ngay trên máy. Chim văng vút lên-phải, ra khỏi màn trong khoảng 0,2 s.
- Cần quay về ngay sau đó. Khoảng 0,35 s sau cú đập, chữ **"OK" bật to** ở giữa-trái (dưới cầu) rồi mờ dần.
- **Đu rất rộng**, gần nằm ngang ở hai đầu: bên trái đu tới sát dưới cầu, bên phải lên tới trên đầu máy. Chu kỳ khoảng 1,1 s.
- Con kế tiếp đi ra đầu cầu, nhảy ôm dây, **đu xuống bên trái trước** rồi mới qua phải.

**Pha 2, cơ chế thật:**
- Chim phá **giàn băng trên cao**, nằm trên mảnh còn lại vài trăm ms rồi rơi tiếp.
- Camera **đi xuống theo chim**. Chim đè 2 gấu, hiện ×2. Gấu bị hạ **rơi xuống nước bên dưới mặt đất**.
- Chim lăn tiếp, camera **lia sang phải** sang một phần đấu trường khác (có cờ). Đấu trường rộng khoảng 2 màn hình.

**Đã sửa trong Pup Launch:**
- Máy bắn đập bằng quả cầu gắn trên cần (`WhackerMachine.Swing` thử va chạm dọc cung quét). Bỏ cầu bay và bỏ
  "cầu bay trúng con đang chờ".
- Biên độ đu 46°–57°.

**Chưa làm:** đấu trường rộng 2 màn hình, camera lia ngang theo chim, nước dưới mặt đất. Đây là thay đổi lớn về
màn chơi, cần vẽ lại các màn.

## 14. Chơi lại bản flashmuseum lần 4, màn 1–2 (12/11/2026, đêm) — so với Pup Launch

Khung game 617×560 (iframe `player.flashmuseum.net`, phải kéo iframe lên đầu trang mới thấy HUD). Chụp ~0,3–0,5 s/khung.

### 14.1 Pha 2: bố cục thật
- **Vách đá cuội nằm bên TRÁI**, có mũ tuyết, dốc chéo ~65° xuống sàn ở khoảng 15% chiều ngang. Chim bay vào **trên** vách.
- **Bên phải KHÔNG có tường**: trời mở, có **gờ băng nổi** (ice shelf) lơ lửng ở mép phải với một con gấu đứng, bị cắt một
  nửa ở mép màn. Camera không lia ngang; **lia dọc theo chim khi lao** (xem §15.1, sửa lại nhận xét "cố định" cũ).
- Sàn: dải đá cuội + đất mỏng, phía sau là nước và tảng băng nhạt. Trời cam, mây vệt đỏ.
- Khối băng xếp **trực tiếp trên sàn** thành cổng vòm (cột 2–3 khối, dầm ngang 3 khối), gấu đứng trên dầm và dưới vòm.
- Chim bay ngang ở ~20% từ đỉnh màn; thân chim ~12% chiều ngang màn, gấu ~11% ngang, 16% cao; khối băng ~7%.

### 14.2 Pha 2: cú lao (đo từng khung)
- Chạm → chim **nghiêng cắm đầu (~70°)** và rơi **nhanh**: ~130 px (≈23% chiều cao màn) trong một khung ~0,35 s, tức
  **khoảng gấp đôi tốc độ bay**, có trôi về trước chút ít (dx ≈ 55 px khi dy ≈ 130). Không có pha "rơi chậm".
- Trúng gấu: gấu **lật ngược (mắt X), văng ngược về sau**, bộ đếm "Total" và "0x/03" tăng ngay. Chim **nảy ngược lại**
  (bị hất về trái) rồi **cuộn tròn**, rơi tiếp, đập vào khối băng khác vẫn phá tiếp.
- Khối băng vỡ thành **mảnh nhỏ nằm lại** trên sàn/dầm đến hết màn (sân "đầy rác băng" dần).
- Chim chạm đất: đứng dậy, **choáng (sao quanh đầu)** cạnh vách trái, rồi biến mất khi chim kế vào.
- Hết mục tiêu (03/03) mà còn chim: chim **vẫn bay vòng** đến khi chạm hoặc hết lượt; màn chỉ kết thúc khi dùng hết chim.
- Chạm khi chim đang ở sát mép trái/đang vòng lại **bị nuốt** (3 lần), phải chạm khi chim đã vào trong màn.

### 14.3 Pha 1, HUD, thẻ
- HUD: trái mặt chim + "x04"; giữa 4 nút xanh (loa, nhạc, ?, pause); phải mặt gấu + "Total 04" và "00/03". Nút ? mở
  Instructions ngay trong màn; pause là nút, không có phím P.
- Thẻ nhiệm vụ: chim **chào rồi cúi nhặt mũ đội lên** (vòng ~4 khung).
- Kết quả: "Mission 2 / Congratulations!", chim đứng trên bụng gấu nằm, "Bears hit: 05/03", "Total bears: 09", Continue.

### 14.4 Đã sửa vào Pup Launch (12/11 đêm)
| CPC | Pup Launch trước | Sửa |
|---|---|---|
| Vách đá trái, phải mở | tường đá to bên phải, trái trống | vách cuội trái (khối 4.2 + dốc 1.8), bỏ tường phải, khung nhìn lệch trái 0.5 |
| Lao dốc ~65–70°, nhanh ×2, có trọng lực | lao chéo 45° chậm bằng lúc bay | `diveAngle` 65, tốc độ = 2 × tốc độ bay (`diveSpeedScale`), `diveGravity` 0.4 |
| Mảnh băng nằm lại | chỉ hiệu ứng bay rồi tan | `Fx.Rubble`: 4 mảnh ở lại chỗ vỡ |
| Gờ băng nổi mép phải, gấu đứng, bị cắt nửa | không có | `TerrainShape.IceShelf` + `Design.EdgePerch` ở x 13.6 (màn 1, 3, 7) |
| Gấu bị hạ văng ngược xa | bay lên gần tại chỗ | `KnockUp` 7, ngang 1.8–3, cản 0.6 |
| Thẻ: chim cúi nhặt mũ rồi chào | chỉ chào | vòng khuỵu (`pup_leap_0`) → chào (`pup_salute`) ×2 |

## 15. Chơi lại lần 5, toàn bộ màn hình (12/11/2026, đêm muộn) — giao diện và đồ họa

Iframe kéo lên toàn cửa sổ (1286×642, game 706×642). Chụp từng màn hình, so với Pup Launch.

### 15.1 Luồng màn hình và giao diện
- **Intro** ~25 s, nút "Skip" dưới-phải. **Tiêu đề**: trời cam hoàng hôn, mây vệt trôi sang phải, sàn băng trôi; logo
  lớn trên; nút **Play** xanh lá bóng to ở giữa có icon chim; "Play More Games" dưới; 3 nút vuông bo xanh ngọc (loa, nhạc,
  ?) trên-giữa. Chữ: trắng đậm, viền xanh đậm; số: xanh dương viền trắng.
- **Instructions** (sau Play lần đầu): panel xanh nhạt bo góc viền xanh dày, 2 cột "Catapult" (máy bắn có khói) và
  "Diving" (chim bay + gấu), nút Continue.
- **Bản đồ**: không HUD ngoài 3 nút; các ô là **đá "?"**; ô đã qua = **đĩa sao vàng**; chim đứng trên **đĩa xanh lá** ở
  ô kế (vẫy tay); lồng chim = boss; gấu đứng ở góc; thỉnh thoảng một cụm hơi bốc trên băng; không số màn, không bộ đếm.
- **HUD pha 1**: trái mặt chim tròn trắng + "x 06" xanh dương to; giữa 4 nút (loa, nhạc, ?, pause). **Không có điểm.**
  Pha 2 thêm bên phải: mặt gấu tròn, "Total 00" nhỏ trên, "00 / 02" to dưới.
- **Hố bắn** (ảnh `cpc_ref_pit_full.png`): hộp đá cuội kín: trần kín cả chiều ngang có nhũ băng, **góc trên-phải trần cắt
  chéo** đi lên làm lối thoát; tường phải là cột đá liền từ sàn; nền trong hộp là làng lều tuyết xanh nhạt với mây hồng.
  Chim bắn trượt bay lên **mắc vào nhũ băng** (treo, vẫy). Chim hàng chờ xếp trên cầu gỗ, con đầu bước ra nắm dây.
- **Chuyển pha**: **màn nước xanh** có mép lượn sóng kéo từ trên xuống phủ hố, rồi panel power-up (bản web), Continue.
- **Thẻ nhiệm vụ**: "Mission 1", chim cúi nhặt mũ đội lên rồi đứng dang cánh; "Hit 02 bears to continue to the next
  level"; "Total bears: 00" (tổng mọi màn); nút Continue; phải bấm mới vào pha 2.
- **Đấu trường màn 1**: trời cam, biển có băng trôi xa; sàn đá cuội viền vàng; **gò đá cuội** giữa-phải có gấu trên; dốc
  băng tam giác; **gờ băng nổi** góc trên-phải có khối băng; tường đá mỏng mép trái; gấu đứng dưới sàn phải.
- **CAMERA PHA 2 KHÔNG CỐ ĐỊNH** (sửa §14.1): khi chim lao xuống, màn **trượt xuống theo chim** (sàn lên gần giữa màn,
  lộ phần đá băng dưới sàn), rồi trượt lên lại khi con kế bay vào. Không lia ngang. HUD đứng yên.
- **Kết quả**: panel "Mission 1 / Congratulations!", chim đứng trên bụng gấu nằm, "Bears hit: 03/02", "Total bears: 03",
  Continue. Không sao, không điểm. Về bản đồ: ô vừa qua thành đĩa sao, chim nhảy sang ô kế.
- **Pause**: màn xám mờ, chữ "PAUSE" trắng to, nút "Play More Games"; bấm pause lần nữa để tiếp.

### 15.2 So với Pup Launch và đã sửa (12/11 đêm)
| CPC | Pup Launch trước | Sửa |
|---|---|---|
| HUD: mặt tròn + số to, không điểm | viên thuốc + hộp điểm/thanh sao | `hud_pup`/`hud_bear` tròn + "× N"; bỏ hộp điểm; "Tổng" + "d/r" bên phải chỉ ở pha 2 |
| Thẻ nhiệm vụ chờ Continue, "Total bears" tổng mọi màn | tự biến mất sau 2,2 s, "Gấu trong màn" | nút Tiếp tục (chạm cũng được), tổng mọi lần chơi |
| Kết quả "Mission N / Congratulations!" + Continue | ruy băng "Hoàn thành!", Next | tiêu đề 2 dòng, Tiếp tục vàng, giữ sao + điểm của game |
| Pause chỉ chữ PAUSE | panel 2×2 | chữ "TẠM DỪNG" to trên nền tối + hàng nút |
| Bản đồ: đá "?", đĩa sao, chim đứng trên đĩa xanh | nút số + sao, chim nhảy cạnh | `node_stone`/`node_star`/`node_glow`, hải cẩu đứng trên ô kế; bỏ số, chú thích, tên vùng |
| Tiêu đề: logo trên, Play to giữa, 3 nút trên-giữa | logo phải, cột nút phải | logo giữa trên, Play vàng to giữa, hàng 3 nút phụ, loa/nhạc/? trên-giữa |
| Instructions sau Play lần đầu | không | thẻ Cách chơi với Tiếp tục (`seenHowTo`) |
| Tường phải hố = cột đá liền | khúc đá nâng dán lên tường vẽ | kéo giãn tường ngay trong tranh (`raise_wall`), cùng loại đá xám |
| Camera theo chim xuống | đã có (`FollowDown`) | không đổi; §14.1 ghi "cố định" là sai |

Chưa làm: màn nước lượn sóng khi chuyển pha (game có wipe riêng), chim mắc nhũ băng đã có từ trước, "Play More Games"
không áp dụng, trần cắt chéo (tranh hố giữ nguyên).

## 16. Học lại CPC lần 6: cơ chế, hoạt ảnh, UI, đồ họa (13/11/2026)

Chơi trọn màn 1 trên flashmuseum, chụp liên tiếp ~0,25 s/khung (game 706×642 trong cửa sổ 1286×642).

### 16.1 Cơ chế pha 1 (đo trên khung hình)
- **Lối thoát là cả khoảng hở bên phải**, giữa đỉnh tường phải (≈ 47% chiều cao màn) và trần chéo. Chim trúng đạn bay
  **thấp và dẹt (~25–35°) sang phải**, ra khỏi màn ở ≈ 30–45% chiều cao. Không phải "khe hẹp trên cao".
- Bắn trượt nhịp: chim bay **dựng đứng** lên, **dính vào nhũ băng trần** và treo đó đến hết màn (2 con dính cạnh nhau).
- Dây: neo ở đầu cầu gỗ, dài ≈ 30% chiều cao màn, đu ±45°, đáy cung ngay trên cốc máy bắn; một chu kỳ ≈ 2 s.
- Máy bắn: cần gỗ quay quanh trục trên khung 2 bánh, cốc chứa **cầu tuyết to** bên trái, lính đội mũ cầm cần bên phải.
  Chạm: cần vung lên ≈ 0,25 s, cầu tuyết lên tới tầm chim, có khói ở đầu cần; rồi hạ về ≈ 0,5 s.
- Sau khi chim rời dây: **vòng lấp lánh vàng** ở đầu dây; chữ **"OK"** cam viền trắng, nghiêng, to ≈ 25% màn, hiện ≈ 0,5 s
  giữa-trái; bộ đếm trừ 1 ngay.
- Con kế: bước tới đầu cầu, **tụt xuống dây**, bắt đầu đu; cả hàng dịch sang phải. Các con chờ có hoạt ảnh riêng
  (ngó, vẫy, giơ mũ).
- Hết chim: **một con chim khổng lồ kéo tấm màn nước xanh từ trên xuống** phủ màn, logo hiện trên nền xanh, rồi panel
  "4 power-ups" + thanh Loading → nút Continue.

### 16.2 Cơ chế pha 2
- Chim bay **ngang tuyệt đối** ở ≈ 21% chiều cao, tốc độ đều: qua hết màn ≈ 3 s; ra mép phải thì **vào lại từ mép trái**,
  cùng độ cao, lặp đến khi chạm.
- Chạm: nghiêng đầu ≈ 55–60° trong 1 khung, có **3 vạch tốc độ trắng**, rơi nhanh ≈ 2,5× tốc độ bay.
- Trúng khối băng / gấu: **mũ văng ra thành vật riêng** (rơi, có lần đội lên đầu gấu), chim **cuộn thành quả bóng đen**
  có **đuôi khói xám 3 chấm**, **nảy cao** (≈ 30% màn) và bay tiếp theo đường cong, nảy nhiều lần; trúng thêm gấu vẫn tính.
- Khối băng bị trúng: vỡ **mảnh xanh nhỏ lơ lửng tại chỗ** một lúc. Gấu bị hạ: biến mất khỏi sàn (văng), bộ đếm tăng.
- Dừng: chim đứng dậy, dang cánh, **khói bốc trên đầu**, đứng đó đến khi con kế vào. Chim trúng sườn gò để lại **vết lõm
  đen** trên đá.
- **Camera trượt xuống theo chim** khi lao thấp (lộ băng xanh dưới sàn), **giữ nguyên ở dưới** trong khi chim còn nảy.
- Vượt chỉ tiêu vẫn chơi tiếp (03/02); màn chỉ kết thúc khi hết chim.

### 16.3 Hoạt ảnh nhân vật
- Gấu: mỗi con một vòng riêng 1–2 s: đấm bốc 2 tay, **ăn từ bát xanh** (há miệng đỏ), ngửa cổ nhìn trời, xoa bụng,
  chỉ tay cười. Gấu **quay đầu nhìn theo chim** đang bay.
- Chim: bay = nằm ngang, chân cam duỗi sau; lao = thẳng người; nảy = bóng; đứng = dang cánh. Mũ xanh lính ở mọi tư thế.

### 16.4 Đồ họa và UI
- Nét viền đen dày, màu phẳng + 1 lớp bóng; đá cuội nhiều màu (xám, nâu, xanh) viền **vàng cát** trên mép; băng xanh
  ngọc có vân tam giác. Trời pha 2 cam đậm → vàng, mây **vệt lửa** trôi chậm sang trái; biển xanh nhạt, băng trôi xa.
- Hố pha 1: nền **làng lều tuyết xanh nhạt + mây hồng** (ngoài trời, sáng), không phải hang tối.
- HUD: số **xanh dương viền trắng**, chữ "Total" trắng nhỏ; nút vuông bo xanh ngọc đậm; mặt nhân vật trong vòng trắng.

### 16.5 Pup Launch còn khác (chưa sửa)
| CPC | Pup Launch | Việc cần |
|---|---|---|
| Thoát qua khoảng hở rộng bên phải, bay dẹt 25–35° | khe trên-phải, bay dốc 48° | hạ `strikeAngle`, mở lối thoát theo cạnh phải; cần vẽ lại tranh hố |
| Bắn trượt → dính nhũ băng, treo tới hết màn | có kẹt, cần so lại thời lượng | giữ con kẹt đến hết pha 1 |
| Chuyển pha: nhân vật kéo màn nước xuống + logo | wipe sóng dâng | làm màn kéo xuống có hải cẩu |
| Bay ngang đều ≈ 3 s/màn, vòng lại cùng độ cao | độ cao/tốc độ đổi theo cú bắn pha 1 | bỏ liên kết pha 1 → pha 2 hoặc thu hẹp |
| Mũ văng thành vật riêng khi trúng | không | thêm mũ rơi |
| Nảy cao ≈ 30% màn, đuôi khói | nảy thấp hơn | tăng `bearBounce`, thêm đuôi khói |
| Camera ở lại dưới khi còn nảy | theo y của hải cẩu | giữ thấp tới khi dừng |
| Vết lõm trên đá chỗ trúng | không | decal |
| Hố sáng ngoài trời (lều tuyết) | hang tối đá xám | vẽ lại `pit_bg` |

## 17. Học lại CPC lần 7: màn 1–3, cách dựng màn và những thứ CPC không có (13/11/2026)

Chơi màn 1, 2, 3 trên flashmuseum (game 706×642).

### 17.1 Cách thiết kế màn
- Chỉ tiêu tăng 1 mỗi màn: màn 1 hạ 2, màn 2 hạ 3, màn 3 hạ 4. Mỗi màn 6 chim ở pha 1. Số gấu trong màn nhiều hơn chỉ
  tiêu 1–2 con, nên luôn có dư để "Perfect".
- **Khối băng đứng yên, không có vật lý**: trúng khối nào thì khối đó vỡ, các khối bên cạnh vẫn lơ lửng tại chỗ (dầm mất
  một đoạn giữa, hai đầu vẫn treo). Không có tháp đổ sập.
- Công trình ghép từ **khối vuông + khối nêm tam giác** ở đầu dầm và chân cột (vát 45°). Màn 2: cổng vòm 2 cột 3 khối +
  dầm 5 khối, 2 gấu trên dầm, 3 gấu dưới vòm, 1 gấu trên gờ băng nổi phải. Màn 3: cột cao 6 khối bên trái, dầm chữ T, bậc
  thang băng xuống phải, gấu trên dầm, trên bậc và dưới sàn.
- Gấu đứng dưới vòm được dầm che: phải phá dầm trước hoặc nảy từ gấu khác vào. Đây là câu đố chính của màn.
- **Một chim có thể hạ 3 gấu** nhờ nảy (màn 2: 03/03 bằng con đầu). Nảy là cơ chế ghi điểm chính, không phải phụ.
- Sàn đấu trường lõm hình chảo: vách đá dốc hai bên, sàn phẳng ở giữa; chim rơi vào chảo sẽ lăn về giữa.
- Hố pha 1 màn 2 (ảnh chụp): máy bắn nằm trong **hốc trũng** giữa hai khối đá cuội, cầu gỗ bên trái trên, trần băng xanh
  có nhũ bên trái, trần đá cuội **cắt chéo** bên phải; nền là thành phố lều tuyết. Bố cục hố giống màn 1.

### 17.2 Bản đồ và luồng
- Sau khi qua màn: ô thành **đĩa xanh có sao vàng**, chim đứng ở ô kế, **giơ mũ lên vẫy**.
- Chữ **"HELP!"** đỏ nháy cạnh lồng chim gần nhất.
- Thẻ nhiệm vụ: chim đứng trên **mũ lính úp** làm bệ. Thẻ kết quả giống hệt mọi màn.
- Panel "Check out all 4 power-ups" (Twins, Rubber Penguin, Penguin on Fire, Ninja Penguin) hiện giữa hai pha ở mọi màn
  (bản web).

### 17.3 Pup Launch có mà CPC không có
| Pup Launch | CPC | Trạng thái |
|---|---|---|
| Thanh máu boss | không có | **đã bỏ** (13/11) |
| "Vua Gấu" to gấp đôi, trèo lại bệ, 3 mạng qua nút | boss cỡ gấu thường, trúng trực tiếp 2 lần | **đã sửa** (13/11) |
| Khối băng có vật lý, tháp đổ | khối đứng yên | **đã đổi** (13/11): `Block` là Static |
| Sao 1–3, điểm, cửa hàng nâng cấp bằng sao | không có; power-up phát theo màn | giữ sao + cửa hàng (người dùng 13/11); chỉ bỏ điểm |
| Thanh gợi ý đáy màn + bàn tay chỉ | không có (chỉ băng chữ ở màn boss) | giữ cho màn dạy chơi |
| Thẻ "Level N + tên vùng" đầu màn | không có | chưa đổi |
| Tuyết rơi suốt màn | không có | chưa đổi |
| Màn bungee, hang ngầm, TNT, vòng bật | chưa thấy ở màn 1–3 | giữ |
| Đường bay pha 2 đổi theo cú bắn pha 1 | bay ngang cố định | **đã đổi** (13/11): bay ngang cố định |

## 18. Va chạm pha 2, xem từng khung ~0,25 s (13/11/2026, tối)
- **Trúng gấu**: sao vàng bung quanh gấu, gấu lộn nhào, khung kế tiếp gấu đã **rơi xuyên sàn** (thấy nửa người dưới mép
  sàn), khung sau nữa biến mất. Bộ đếm tăng ngay kèm sao ở HUD. Gấu **không nằm lại** trên sân.
- **Chim sau cú trúng**: bật ngược lại **thấp**, đỉnh cung chỉ cao bằng khoảng một con gấu, có 2–3 chấm khói; bay ngang
  về phía vách trái, đập vách (chớp trắng tròn), rơi xuống chân vách với **vòng bụi tròn**, nảy một nhịp rất nhỏ rồi đứng.
- **Trúng sườn gò đá** (không trúng gấu): chớp trắng, bật ngược thấp, **trượt/lăn theo dốc** xuống sàn, đập khối băng thì
  khối vỡ, dừng trong bụi. Gấu đứng cạnh không bị ảnh hưởng.
- **Trúng dầm băng trên cao**: 2 khối vỡ thành mảnh xanh lơ lửng, mũ rơi riêng xuống sàn, chim nảy lên nhẹ rồi lượn xuống.
- Chạm lúc chim còn sát mép trái bị bỏ qua.

## 19. Các loại khối và độ bền (13/11/2026, xem video "Crazy Penguin Catapult (2008) - Flash Version", màn 1–16)
- **Chỉ có một loại băng** (khối vuông xanh và khối nêm). Mọi lần chim chạm, kể cả khi đang bay ngang hay đang lăn, khối
  đó **vỡ ngay lần đầu** thành mảnh vuông; không thấy khối nào nứt rồi còn đứng. Không có băng "cứng" hay băng nhiều lớp.
- **Thùng gỗ** chỉ thấy ở màn boss (màn 10): làm cầu trên và dưới chỗ boss đứng. Chim đập vào không vỡ. Khi chim đáp lên
  **nút xanh**, nút đổi đỏ và **cầu thùng gỗ phía trên biến mất**.
- Đá cuội (gò, vách, trần) và lớp băng nền dưới sàn là địa hình, không phá được.
- Đổi sang chơi trực tiếp bản flashmuseum không được: tiến độ đã bị xoá, chỉ mở màn 1, và chạm mù thua màn 2.

## 20. Chơi tay màn 1–5 trên flashmuseum (13/11/2026, chiều) — khối, vật lý, va chạm
Chơi trực tiếp, chụp ~0,1–0,5 s/khung quanh từng cú va. Thua màn 5 thì game xoá hết tiến độ về màn 1, nên dừng ở đây.

### 20.1 Khối
- **Màn 1–5 chỉ có một loại băng** (khối vuông + khối nêm). Mọi lần chạm đều vỡ ngay, kể cả khi chim **bay ngang** chưa lao:
  chim phá khối và **bay xuyên qua tiếp**, không đổi hướng, không rơi.
- Khối không có trọng lực: khối bên dưới vỡ thì khối bên trên vẫn treo.
- Lớp băng nền dưới sàn và đá cuội là địa hình.

### 20.2 Va chạm và độ nảy
- **Lao trúng gấu**: gấu lật ngửa (mắt X), **rơi xuyên sàn** ra khỏi màn; chim bật lên thấp (≈ 1 thân chim) và ngược về sau.
- **Lao trúng khối băng**: khối vỡ, chim **nảy ngược sang trái chỉ ≈ nửa thân chim**, rồi rơi tiếp.
- **Lao trúng mặt dốc đá**: chim **không nảy mà trượt bụng xuống dốc**, có **vệt lửa ma sát** cam; trượt qua gấu vẫn hạ gấu.
  Màn 4: một con hạ 3 gấu nhờ trượt xuống dốc vào hang (×3).
- **Chim dừng**: đứng dậy bằng chân, choáng (sao) tại chỗ, rồi biến mất khi con kế vào.
- **Rơi vào hang/khe**: camera trượt xuống theo, gấu dưới hang bị hạ khi chim rơi trúng.
- Hết chim nhưng con cuối chưa lao thì nó **vẫn bay vòng** đến khi chạm.

### 20.3 Màn
| Màn | Cần | Bố cục |
|---|---|---|
| 1 | 2 | gò giữa + gờ băng nổi phải, 5 gấu |
| 2 | 3 | cổng vòm băng (2 cột 3 khối + dầm 5 khối, nêm hai đầu), 6 gấu |
| 3 | 4 | tháp băng chữ T + dầm dài sang phải, hang bát giác dưới sàn |
| 4 | 4 | dốc đá có hang bên dưới, dầm băng ngang, gấu dưới hang |
| 5 | 4 | sàn có khe giữa 2 bờ, khung băng chữ Γ trái, tháp băng phải |

## 13. Vẫn chưa rõ

- Panel thua bản web: **đã có mô tả từ FrostFling (10.1)**; mình chưa tự thấy.
- Pha 1: **đã rõ (11.1)**, 1 lần chạm, 1 bố cục, trên cả bản Snokido và flashmuseum.
- Đĩa thưởng góc phải cho gì (9.11). Ninja: chỉ có mô tả bản Java (10.5); bản web chưa thấy chạy.
- Cơ chế nút boss: thấy nút đổi xanh → đỏ và cầu thùng gỗ biến mất (10.6), chưa rõ quan hệ nhân quả.
- Boss 2–5 và boss cuối bản web (không video nào có). Bản Java: 3 boss bụng màu (10.8).
- Âm thanh (công cụ không nghe được).
