# Phát hành lên Google Play (lần đầu)

Danh sách việc theo thứ tự, chép cách làm của repo one-line-fill (`docs/RELEASE.md` bên đó, 2026-11-13). ✋ = anh/chị tự
làm (cần tài khoản, tiền, danh tính). 🤖 = Claude làm khi có thông tin.
Quy trình kỹ thuật (checks → ship review → push → deploy → QA) ở `CLAUDE.md` và `~/.claude/rules/ship-push-deploy-qa.md`.

## 0. 🤖 Git ✅
Đã có repo: `origin` = https://github.com/laD02/pup.git, nhánh `main` (2026-11-13, commit đầu `7e3434a`).
Build AAB chỉ từ cây sạch, đã commit và push (`git status` trước).

## 1. ✋ Email
Dùng chung email của One Line Fill: `lehoang10112002@gmail.com` (AdMob, Play Console, trang liên hệ).

## 2. 🤖 Trang web ✅ soạn xong, chờ đăng
`site/` (2026-11-13): `index.html` (en) + `vi/ de/ fr/ es/ it/ pl/` (một trang mỗi ngôn ngữ, `hreflang`, sinh bằng
`python3 -I site/make_pages.py`), `privacy.html` (vi + en, có mục GDPR), `sitemap.xml`, `robots.txt`, `icon.png`.
Đăng ở `https://lad02.github.io/pup/`: chép nội dung `site/` (trừ `make_pages.py`) vào thư mục `pup/` của repo
`laD02/laD02.github.io`, push.
Thêm dòng `Sitemap: https://lad02.github.io/pup/sitemap.xml` vào `robots.txt` ở gốc repo đó. Không cần `app-ads.txt` mới:
file ở gốc `https://lad02.github.io/app-ads.txt` đã có publisher `pub-5420799226073285` (`docs/ASO.md` §7).

## 3. AdMob → 🤖 điền ID
Tài khoản AdMob cá nhân của One Line Fill (publisher `pub-5420799226073285`, Gmail `lehoang410112002`, loại **Cá nhân**)
dùng chung cho Pup Launch.
1. ✅ 2026-11-13: Apps → Add app → Android → "Chưa có trên Google Play" → **Pup Launch**.
2. ✅ Ad unit theo quy tắc quảng cáo của game (`GAME_DESIGN.md`): **Interstitial** (thỉnh thoảng giữa các màn) và
   **Rewarded** (tự chọn xem). Không có banner.
   - App ID: `ca-app-pub-5420799226073285~5657352369`
   - Interstitial: `ca-app-pub-5420799226073285/8091944013`
   - Rewarded: `ca-app-pub-5420799226073285/6778862341`
3. 🤖 Các ID trên nằm trong `Assets/Scripts/Ads/AdsConfig.cs` (App ID cả trong cấu hình Android của Google Mobile Ads
   plugin, theo `README.md`).
   Bản build test và emulator luôn dùng ID test của Google; không bấm quảng cáo thật của chính mình.
4. ✋/🤖 Privacy & messaging → **GDPR message** (UMP) cho app Pup Launch, xuất bản. Không có message này thì người dùng EU
   không thấy form đồng ý và AdMob hạn chế quảng cáo ở EU.
5. Sau khi app lên Play: liên kết app AdMob với app trên Play (AdMob → App settings → Link to app store). AdMob tự đọc
   `app-ads.txt` ở website của tài khoản nhà phát triển (bước 2).

### 3b. Thông báo GDPR (EU) ✅ 2026-11-14

AdMob (tài khoản `authuser=2`, cùng tài khoản One Line Fill) → Quyền riêng tư và thông báo → Quy định của Châu Âu →
**"Pup Launch - EU consent"**, đã xuất bản: tiếng Anh mặc định + de, fr, es, it, pl (AdMob không có tiếng Việt cho thông
báo EU; người chơi VN không thấy thông báo này). URL privacy `https://lad02.github.io/pup/privacy.html`. Lựa chọn:
Đồng ý, Quản lý lựa chọn, **Không đồng ý bật cho mọi nước** (CNIL: từ chối phải dễ như đồng ý). Không có thông báo này
thì form UMP trong game không hiện ở EU và quảng cáo EU gần như không chạy. Thông báo mới cần tới 1 giờ mới hiện trong app.
Sau khi app lên Play: AdMob → Ứng dụng → Pup Launch → Thêm cửa hàng (liên kết Play) để AdMob duyệt app.

## 4. ✋ Tài khoản Google Play Console
Dùng tài khoản "Le Hoang Games" (của One Line Fill). Tài khoản cá nhân tạo sau 11/2023 phải **thử nghiệm kín ≥ 12 người
trong ≥ 14 ngày** cho mỗi app mới trước khi được phát hành công khai. Chuẩn bị danh sách email người thử.

## 5. 🤖 Build production + nộp lên Play
1. ✋ Tạo upload keystore một lần, giữ ngoài git (`README.md` "Release on Google Play").
2. 🤖 Khi đã có ID AdMob thật (bước 3): đổi `[Unreleased]` trong `CHANGELOG.md` thành `## [x.y.z] — <ngày>`, tăng
   `Version` + `VersionCode` trong `ProjectBuilder` (hoặc biến môi trường `PUFFER_VERSION_CODE`), build AAB bằng
   `ProjectBuilder.BuildAab` với biến môi trường `PUFFER_KEYSTORE*` (`README.md`).
3. 🤖 Cài đúng bản đó lên emulator và chơi thử theo `CLAUDE.md` "QA on the shipped build".
4. ✋ Lần đầu: tạo app trong Play Console (tên mặc định `Pup Launch: Seal Catapult`, ngôn ngữ mặc định en-US, Game, Miễn
   phí), kéo thả file `.aab` vào track **Kiểm thử nội bộ**. Play App Signing: bật (Play giữ khoá ký, mình giữ khoá upload).

## 6. ✋ Điền thông tin trong Play Console (Claude soạn sẵn bên dưới)

### Store listing
Toàn bộ chữ (tên, mô tả ngắn, mô tả đầy đủ en + vi), từ khoá, chữ trên ảnh chụp: **`docs/ASO.md` §3–4**.
Thêm listing **de-DE, fr-FR, it-IT, es-ES, pl-PL, en-GB, vi-VN** bên cạnh en-US mặc định, theo thứ tự ở `docs/ASO.md` §8
(Play Console → Store presence → Main store listing → Manage translations → bản dịch của mình, không dùng dịch máy).
- **Ảnh:** icon 512×512 (cần làm, `docs/ASO.md` §4), feature graphic 1024×500, 6–8 ảnh chụp **ngang** theo `docs/ASO.md`
  §4 (🤖 chụp từ emulator).
- **Danh mục:** Trò chơi → **Arcade**. **Tags:** Arcade, Casual, Physics, Offline. **Website:** `https://lad02.github.io/`
  (gốc, để AdMob đọc `app-ads.txt`). **Email:** bước 1.

### App content
- **Privacy policy:** `https://lad02.github.io/pup/privacy.html`
- **Ads:** Có, ứng dụng chứa quảng cáo.
- **App access:** Không cần đăng nhập, mọi tính năng dùng được ngay.
- **Target audience:** 13+ (chọn 13–15, 16–17, 18+). Không chọn dưới 13 (khi đó phải theo Families policy và AdMob phải
  bật cờ trẻ em). Game hoạt hình dễ thương nên Play có thể hỏi "có hấp dẫn trẻ em không": trả lời thật, nếu Play xếp vào
  nhóm trẻ em thì phải đổi cấu hình quảng cáo trước khi phát hành.
- **Content rating (IARC):** bạo lực hoạt hình nhẹ (gấu bị "hạ" là lộn nhào choáng, không máu: chọn "cartoon/fantasy
  violence" mức nhẹ nếu câu hỏi có), ngôn ngữ, cờ bạc… đều "Không"; có quảng cáo. Thường ra Everyone / 3+ hoặc 7+.
- **Data safety** (chỉ do AdMob SDK; game không có thống kê riêng):
  - Thu thập: **Device or other IDs** (Advertising ID); **App activity** (tương tác quảng cáo);
    **App info and performance** (chẩn đoán, crash của SDK quảng cáo); **Location → Approximate** (từ IP).
  - Mục đích: Advertising or marketing; Fraud prevention, security.
  - Chia sẻ với bên thứ ba: Có (Google, cho quảng cáo).
  - Mã hóa khi truyền: Có. Người dùng yêu cầu xóa: dữ liệu game nằm trên máy (gỡ app là xóa);
    dữ liệu quảng cáo do Google quản lý (đặt lại Advertising ID).
  - Dữ liệu game (sao, nâng cấp, cài đặt) **không** khai vì không rời khỏi máy (`site/privacy.html`).
- **Government apps / Financial / Health:** Không.

## 7. Mediation (để sau khi có vài trăm người chơi/ngày)
Như one-line-fill §8: thêm AppLovin + Unity Ads bidding trong AdMob Mediation, thêm adapter vào Unity, cập nhật
`app-ads.txt` ở gốc, `site/privacy.html` và Data safety.
