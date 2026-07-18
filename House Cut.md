# ANTE / MORTEM — GDD v1.3

**48 SAATLİK GAME JAM**
PS2 ESTETİKLİ TEK ODALI KUMAR / BEDEN KORKUSU
*"Masanın borcu parayla kapanmaz."*

| | |
|---|---|
| **TEMA** | Bedel Ödemek |
| **FORMAT** | Tek oyunculu / Endless Run |
| **HEDEF** | PC · 10–20 dk koşu |

> WORKING TITLE · OYUN MOTORUNDAN BAĞIMSIZ TASARIM

**v1.3 değişiklikleri:** Bahis ayarlanabilir oldu (yukarı/aşağı ok, ±$50, taban = min bahis). Organ $ değerleri ×$1.000 ölçeğine yükseltildi (başlangıç değeri; oyun içi ayarlanır).
**v1.2:** Tek kaynak modeline geçildi — **Para ($)**. Ayrı Hayati İşlev barı kaldırıldı. Masaya nakitle oturulur; para bitince organlar nakde çevrilir. Vitalite bazlı efektler paraya remap'lendi.
**v1.1:** Tam blackjack (52 kart); her organa debuff'ını dengeleyen bir buff.

---

## 01 / TASARIM ÖZETİ

### Tek Cümlede Oyun
Karanlık bir odada yabancı bir krupiyeye karşı blackjack (21) oynarsınız; paranız bitince her bahsi **kendi organlarınızı nakde çevirerek** karşılar, kalıcı sakatlıklar pahasına mümkün olduğunca uzun süre masada kalmaya çalışırsınız.

> **TASARIM KARARI** — Para geri kazanılabilir ama organlar geri gelmez. Tek yönlü çöküş organ sayısında yaşar; para sadece oynak tampondur. Asıl strateji kartlarda değil, **hangi organı ne zaman bozduracağınızda** ve bunun sonraki ellere bindireceği kalıcı bedelde/faydadadır.

### Ürün Tanımı

| ALAN | KARAR |
|---|---|
| Tür | Psikolojik korku · Kumar · Hayatta kalma / high-score |
| Bakış | Birinci şahıs, masaya sabitlenmiş kamera |
| Oturum | İlk koşu 8–12 dk; ustalaşınca 15–20 dk |
| Platform | PC; klavye + mouse, opsiyonel gamepad |
| İçerik | Tek oda, tek masa, tek rakip, tek kart oyunu |
| Tek kaynak | **Para ($)** — hem bahis hem "can". Ayrı vitalite barı yok. |
| Bitiş | Bozduracak organ kalmayınca (Kalp son ödeme) oyun biter |

### Dört Tasarım Sütunu
1. **Anında anlaşılır:** Oyuncu 30 saniye içinde Kart Çek / Kal kararını öğrenir.
2. **Bedel kalıcıdır:** Para geri gelebilir; organ ve debuff'ı asla geri gelmez.
3. **Sunum oynanıştır:** Shader, nefes, ışık ve kamera bozulması sadece dekor değil geri bildirimdir.
4. **Jam kapsamı korunur:** Yeni oda, ikinci oyun, cerrahi mini oyun ve karmaşık AI yoktur.

> **BAŞARI ÖLÇÜTÜ** — Oyuncu iflas edince "hangi organ en az acıtır?" değil, "hangi bedelle ve hangi avantajla birkaç el daha yaşayabilirim?" diye düşünüyorsa tasarım çalışıyor demektir.

---

## 02 / GAMEPLAY LOOP

### Çekirdek Döngü
Her el 45–90 saniye sürer. Kazanmak para ve kısa bir nefes alanı verir; kaybetmek parayı eritir. Para min bahsin altına düşünce organ satmak zorunda kalırsın — her satış kalıcı sakatlık bırakır. Artan min bahis her zaman evi kazandırdığı için eninde sonunda organlar tükenir.

> *Şekil 1 — Mikro döngü: bahsi karşıla → kart kararı → sonuç → para güncellenir → (para yetmezse organ boz) → daha pahalı yeni el*

### Bir Elin Mikro Akışı
1. **Bahsi ayarla ve karşıla** — Güncel para ve min bahis gösterilir. Oyuncu bahsi **yukarı/aşağı ok ile ±$50** oynatabilir (taban = min bahis, tavan = eldeki para). Para min bahsi karşılıyorsa el başlar. **Para min bahsin altındaysa organ satış ekranı açılır** (aşağıda, 04).
2. **Kartlar dağıtılır** — Oyuncuya iki açık, Yabancı'ya bir açık ve bir kapalı kart gelir.
3. **Oyuncu karar verir** — 15 saniye içinde Kart Çek veya Kal. Süre dolarsa otomatik Kal.
4. **Kasa oynar** — Kapalı kart açılır; Yabancı toplamı en az 17 olana kadar kart çeker (yumuşak 17'de durur).
5. **Sonuç çözülür** — Kazanırsan para +bahis (Blackjack 3:2 öder); kaybeder veya berabere kalırsan para −bahis.
6. **El sonu ekonomisi** — Aktif organ debuff'ları (para sızıntısı) ve buff'ları (galibiyet nakdi) uygulanır.
7. **Zorluk artar** — El sayacı ilerler; **min bahis yükselir**, süre baskısı ve atmosfer yoğunlaşır. Sonraki elde para yetmezse akış 1. adıma (organ satışı) döner.

---

## 03 / MASA KURALLARI

### Kumar: Tam Blackjack (21)

> **NEDEN BLACKJACK?** — İki temel butonla (Kart Çek / Kal) oynanır, herkesin sezgisel olarak anlayacağı kadar tanıdıktır ve 48 saatte güvenilir biçimde uygulanabilir. Oyunun özgünlüğü kart sisteminden değil, para/beden ekonomisinden gelir.

### Kurallar
1. Deste standart **52 karttır**: dört renkte As, 2–10 ve üç resimli kart (Vale, Kız, Papaz). Renk/simge yalnızca görseldir.
2. **Kart değerleri:** 2–10 kendi değerinde; Vale/Kız/Papaz = 10; **As**, eli 21'i geçirmiyorsa 11 (yumuşak el), geçiriyorsa 1 (sert el) sayılır.
3. Oyuncu ve Yabancı ikişer kartla başlar; Yabancı'nın ikinci kartı kapalıdır. **İlk iki kartın toplamı 21 ise bu "Blackjack"tir** ve el anında kazanılır (3:2 öder).
4. Oyuncu istediği kadar Kart Çekebilir veya Kalabilir. 21'i geçmek (batmak) anında kayıptır.
5. Oyuncu kaldıktan sonra Yabancı kapalı kartını açar ve toplamı **en az 17 olana kadar çeker; yumuşak 17'de durur**.
6. 21'i geçmeyen en yüksek toplam kazanır. **Beraberlikte kasa kazanır** (evin payı — uzun vadede parayı garantiyle eritir).
7. Deste 15 kartın altına düştüğünde otomatik karılır; sayım stratejisi tasarım hedefi değildir.

### Para, Bahis ve Ödeme

| | DEĞER | NOT |
|---|---|---|
| Başlangıç parası | **$1.000** | Masaya bu kadar oturursun |
| Min bahis (1. el) | **$700** | Her elin **tabanı** — altına inilemez |
| Min bahis artışı | **+$300 / el** | 1. el $700, 3. el $1.300, 6. el $2.200… |
| Bahsi ayarlama | **±$50 / adım** | Yukarı ok artırır, aşağı ok azaltır (min bahis ↔ eldeki para arası) |
| Galibiyet ödemesi | **+bahis (1:1)** | Blackjack **3:2** öder |
| Kayıp / beraberlik | **−bahis** | Beraberlik kasaya yazılır |

> **BAHSİ AYARLAMAK NEDEN VAR?** — Taban zaten min bahis; oyuncu üstüne çıkarak riskini seçer. Büyük bahis, kaybettiğin parayı hızlı toplar (veya seni hızlı iflasa götürür); tabanda kalmak güvenli ama artan min bahise yavaş yetişir. Bir risk/ödül kolu — ama tavan her zaman eldeki para olduğu için asla borca giremezsin.

> **NEDEN ARTAN BAHİS?** — Min bahis her el yükselir; galibiyet nakdi ve blackjack bonusu bir süre parayı ayakta tutsa da ev payı (beraberlik + artan taban) uzun vadede her zaman kazanır. Böylece iflas ve organ fazı kaçınılmazdır — sadece "ne zaman" değişir.

### Skor ve Erteleme

| SONUÇ | SKOR | OYUN ETKİSİ |
|---|---|---|
| Normal galibiyet | El no × 100 puan | Para +bahis; sonraki ele geç |
| **Blackjack** (ilk iki kartta 21) | +50 bonus | Para +bahis ×1,5; **1 Erteleme** kazan |
| Seri: 3 galibiyet | +100 bonus | Sadece skor; yeni sistem eklemez |
| Kayıp / beraberlik | 0 puan | Para −bahis |

> **ERTELEME** — Blackjack'te kazanılır. Bir kez kullanılır: **sonraki min bahis artışını atlatır** (o el taban yükselmez). Küçük ama iflası bir el geciktiren nefes alanı.

---

## 04 / PARA = CAN & ORGAN EKONOMİSİ

### Tek Kaynak: Para
Ekranda takip edilen tek sayı **paradır**. Para hem bahis birimi hem "can"dır: bittiğinde ölmezsin — **bedenini bozdurmaya başlarsın**. Ayrı bir Hayati İşlev/sağlık barı yoktur. "Ne kadar canlıyım" sorusunun cevabı barda değil, **tabakta biriken organlarda ve üst üste binen debuff'larda** okunur.

### Organ Satışı (para min bahsin altına düşünce)
Para bir sonraki elin min bahsini karşılamıyorsa masada anatomik doğruluk iddiası taşımayan bir "beden defteri" açılır. Oyuncu organ satarak parayı **min bahse eşit veya üstüne** çıkarır.

**Satış Akışı**
1. Uyarı düşer: **PARA YETMİYOR — min bahis: $X**. Krupiye ilk iflasta konuşur: *"Paran yokmuş. Sorun değil."*
2. Oyuncu organ siluetlerinin üzerinde gezinir; her organın **$ değeri**, kalıcı debuff'ı ve buff'ı görünür.
3. Bir veya daha fazla organ seçilir; toplam değer paranı min bahsin üstüne çıkarınca **SAT** aktifleşir.
4. Onaydan sonra kamera gövdeye eğilir; tek ortak çıkarma animasyonu + kararma + ses oynar.
5. Organ düşük poligonlu fizik objesi olarak Yabancı'nın metal tabağına düşer ve orada kalır.
6. Para, satılan organların $ değeri kadar artar; **artan üstü cebinde kalır.** Debuff ve buff kalıcı olarak devreye girer.

> **KAPSAM HİLESİ** — Açık gövde veya organa özel cerrahi animasyon yapılmaz. El + neşter aynı hareketi tekrarlar; organ, kısa siyah kesmeden sonra tabağa düşer. Gore hissini ses, kamera sarsıntısı, decal ve renk verir.

### Kritik Hâl
Ayrı bir vitalite eşiği yerine **düşük likidite** tetikler: para bir min bahsin altındayken **ve** satılabilir 2 veya daha az organ kaldığında Kritik Hâl başlar — nabız, daralan FOV, elde hafif titreme, kırmızı vignette. Krupiye: *"Daha hâlâ fazlan var."*

### Kalp Güvenliği (soft-lock önleme)
Para min bahsin altındaysa ve **Kalp dışında satacak organ kalmadıysa** Kalp otomatik açılır. Kalp $5.000 değerinde son ödemedir; verildiği an koşu biter. Böylece satış ekranında asla kilitlenme olmaz.

---

## 05 / ORGAN MATRİSİ

### MVP Organları: $ Değer, Debuff ve Buff
MVP'de sekiz satılabilir organ + Kalp vardır; böbrek aynı modelin iki örneğidir. Aşağıdaki **$ değerler ×$1.000 ölçeğiyle başlar** — hepsi başlangıç değeridir, denge oyun içi ayarlanır. Debuff'lar rahatsız edici olmalı ama kart rakamını tamamen okunamaz kılmamalıdır. **Her organ ayrıca bir kalıcı buff taşır**; ucuz organlar küçük, pahalı organlar güçlü avantaj verir.

| ORGAN | $ DEĞER | KALICI DEBUFF | KALICI BUFF |
|---|:---:|---|---|
| Kulak | $1.000 | Ses tek kanala düşer; high-cut ve hafif tinnitus. Diyalog altyazısı açık kalır. | Yabancı'nın kapalı kartının yüksek mi (7+) düşük mü olduğuna dair belirsiz bir ses ipucu duyarsın. |
| Dalak | $1.000 | Sonraki her organ satışı **−$300 daha az** getirir (kötü kur); daha uzun kan vinyeti. | Kritik Hâl eşiği düşer (daha düşük parada tetiklenir); iflasa daha dayanıklısın. |
| Böbrek I | $1.000 | Sadece koyu vignette / nabız katmanı; düşük anlık bedel. | **İyi kur:** her organ satışında **+%20 daha fazla** nakit alırsın. |
| Böbrek II | $1.000 | Her el sonu **−$400** (kanama parayı sızdırır); ikinci böbrek için ağır bedel. | Min bahis artışı yavaşlar: taban her el yerine **her 2 elde bir** yükselir. |
| Mide | $2.000 | Her kart çekişinde kısa bulantı dalgası; kartlar 0,6 sn eğilip toparlanır. | Kart Çektiğinde, yeni toplamın 21'e kaç kaldığı bir an vurgulanır (hesap kolaylığı). |
| Sol Göz | $2.000 | Ekranın sol %30'u karanlık maske; hafif çift görüntü ve daha dar görüş. | Yabancı'nın kapalı kartı her elin ilk saniyesinde bir an açık gösterilir. |
| Akciğer | $3.000 | Hırıltı + FOV nabzı; oyuncu karar süresi 15 sn'den 10 sn'ye iner. | Derin nefes: her 3 elde bir, sonraki kararında süre +5 sn uzar. |
| Karaciğer | $3.000 | Kirli sarı renk ayrışması; her el sonu **−$600**. | **Rejenerasyon:** her galibiyette **+$800** nakit — bedeni paraya çeviren tek buff. |
| Kalp | $5.000 | Yalnız başka organ kalmayınca açılır; son satış ve oyun sonu. | Yok — kalp yalnızca oyun sonu ödemesidir; verildiği an koşu biter. |

### Denge İlkeleri
- Ucuz organlar ($1.000) bahsi verimli karşılar ama seni yakında tekrar tekrar satmaya zorlar.
- Pahalı organlar ($3.000) bir satışta birkaç bahis alır; buna karşılık ağır bir debuff yükler.
- Hiçbir organ yalnızca kozmetik değildir; her birinin en az bir kalıcı **debuff'ı ve onu dengeleyen bir buff'ı** vardır.
- El sonu para sızdıran organlar (Böbrek II −$400, Karaciğer −$600) satıldıktan sonra pasif aşağı baskı yaratır — bu, şanslı bir oyuncunun iflasını da garantiler.
- İlk playtestte tek bir "doğru satış sırası" çıkarsa $ değerler ±$500, buff güçleri ±%50 aralığında ayarlanır. (Tüm $ değerler oyun içi ayarlanabilir başlangıç değeridir.)

> **MVP ÖNCELİĞİ** — Zaman daralırsa Kulak, Göz, Akciğer, Böbrek I–II ve Kalp bırakılır; Dalak, Mide ve Karaciğer sırayla kesilebilir. Buff'larda öncelik: peek (Göz), iyi kur (Böbrek I) ve regen nakdi (Karaciğer) önce. Sistem beş eşsiz debuff+buff çiftiyle de çalışır.

---

## 06 / ZORLUK, UX VE KONTROLLER

### Zorluk Eğrisi
Yabancı hile yapan karmaşık bir AI değildir. Zorluk üç şeffaf kaynaktan gelir: **yükselen min bahis**, azalan karar süresi ve oyuncunun kendi seçtiği kalıcı organ etkileri (debuff'lar zorlaştırır, buff'lar hafifletir).

| KADEME | ELLER | MİN BAHİS | TEMEL SÜRE | ATMOSFER / AMAÇ |
|:---:|:---:|:---:|:---:|---|
| I | 1–3 | $700–$1.300 | 15 sn | Temiz masa; nakitle oyna, kuralları öğren |
| II | 4–6 | $1.600–$2.200 | 14 sn | Işık nadiren titrer; para erir, ilk organ satışı |
| III | 7–9 | $2.500–$3.100 | 12 sn | Krupiye daha hızlı; masada organlar birikir |
| IV | 10–12 | $3.400–$4.000 | 10 sn | Kritik hâl olası; satacak organ azalır |
| V | 13+ | $4.300+ | 8 sn | Endless baskı; ölüm kaçınılmaz fakat skor açıktır |

### HUD
- **Para ($):** ekranın kalıcı tek sayacı; her el sonucunda anime olur.
- **Bu elin bahsi / min bahis:** masadaki jeton yığını olarak da gösterilir; ok tuşlarıyla ayarlanan bahis anlık güncellenir, min bahis tabanı ayrıca işaretlidir.
- **Organ tabağı:** verilen organlar Yabancı'nın tabağında görünür kalır — kalan organ sayısı = doom göstergesi.
- **El sayacı:** kademe/süre baskısını sezdirir.

### Kamera ve Etkileşim
- Kamera masaya oturur; oyuncu yalnızca yaklaşık 35° sağ/sol ve 25° aşağı/yukarı bakabilir.
- Kart Çek ve Kal seçenekleri masadaki iki fiziksel buton/jeton olarak da gösterilir.
- Organ satışı sırasında kamera otomatik olarak beden defterine döner; serbest dolaşım yoktur.
- İmleç yerine küçük, titreyen bir merkez noktası kullanılır; seçim vurgusu emissive kenardır.

### Kontroller

| GİRDİ | İŞLEV |
|---|---|
| Mouse | Sınırlı kamera / organ üzerinde gezinme |
| Yukarı ok | Bahsi **+$50** artır (tavan: eldeki para) |
| Aşağı ok | Bahsi **−$50** azalt (taban: min bahis) |
| Sol tık / E | Seç, Kart Çek, organ ekle/çıkar |
| Sağ tık / Space | Kal veya satış ekranında geri |
| Enter | Bahsi/satışı onayla / hızlı yeniden başlat |
| Esc | Duraklat / çıkış |

> **ERİŞİLEBİLİRLİK** — Başlangıç ekranında içerik uyarısı; altyazı varsayılan açık; kamera sarsıntısı ve kromatik ayrışma azaltılabilir. Görsel organ etkileri hiçbir zaman kart rakamını tamamen kapatmaz.

---

## 07 / SUNUM: GÖRSEL, ANLATI VE SES

### Sanat Yönetimi
Tek mekân, PS2 dönemini çağrıştıran düşük çözünürlüklü ve kirli bir fiziksel dünya gibi görünür. Odanın sınırları karanlıkta kaybolur; kompozisyonu masa lambası, Yabancı'nın elleri ve metal organ tabağı taşır.

| KATMAN | KARAR |
|---|---|
| Palet | Kömür siyahı, kirli kemik rengi, oksit kırmızısı, hasta sarı |
| Geometri | Keskin siluetler; elde 800–1.200 tri, Yabancı 2.500 tri, organ 300–700 tri |
| Doku | 128–256 px; nearest filtering, el boyaması kir/kan maskeleri |
| Render | 640×360 veya 640×480 iç çözünürlük, nearest upscale, hafif dithering |
| PS2 efekti | Vertex jitter, düşük hassasiyetli UV/affine hissi, kontrollü color banding |
| Gore | Koyu kırmızı decal, siyah cut, düşük poligon organ; sıvı simülasyonu yok |

### Anlatı Çerçevesi
Oyuncunun kimliği, neden borçlu olduğu ve Yabancı'nın doğası açıklanmaz. Hikâye; birkaç kısa replik, masadaki organ birikimi ve oyuncunun bedensel çöküşü üzerinden okunur. Amaç lore üretmek değil, masadan kalkamama hissidir.

| AN | YABANCI |
|---|---|
| Masaya oturuş | "Otur. Şansın varmış gibi yapalım." |
| İlk iflas (ilk organ) | "Paran yokmuş. Sorun değil." |
| Kritik hâl | "Daha hâlâ fazlan var." |
| Kalp açılır | "Şimdi gerçekten değerli bir şeye geldik." |
| Oyun sonu | "Hesap kapandı." |

### Ses Katmanları
- **Sabit:** floresan uğultusu, uzak metal gıcırtısı, düşük frekanslı oda tonu.
- **Eylem:** kart sürtünmesi, jeton/kemik tıkırtısı, bıçak, ıslak düşüş, masa darbesi.
- **Beden:** nabız, nefes, tinnitus ve organlara bağlı loop'lar ayrı mixer katmanlarında.
- **Müzik:** melodik parça yok; el kademesi yükseldikçe oda tonuna disonant katman eklenir.

---

## 08 / TEKNİK TASARIM

### Durum Makinesi
Oyun tek bir deterministik akış yöneticisiyle kurulmalıdır. Her state yalnızca kendi girişini kabul eder; animasyon bitti sinyali olmadan bir sonraki state'e geçilmez. Bu, jam sırasında çift tıklama ve soft-lock riskini azaltır.

| STATE | SORUMLULUK | ÇIKIŞ |
|---|---|---|
| INTRO | Kısa açılış; ilk deste ve para hazırlanır | BET_CHECK |
| BET_CHECK | Para ≥ min bahis mi? Yeterse bahsi ayarla (±$50, min↔para) ve onayla | DEAL / ORGAN_SALE / GAME_OVER |
| ORGAN_SALE | Organ seçimi, $ doğrulama, kalp fallback | EXTRACTION |
| EXTRACTION | Çıkarma animasyonu + effect uygulama | BET_CHECK veya GAME_OVER (Kalp) |
| DEAL | Kartları üret / animasyonu bekle | PLAYER_TURN |
| PLAYER_TURN | Kart Çek / Kal / sayaç | DEAL veya DEALER_TURN |
| DEALER_TURN | Kapalı kartı aç; 17'ye kadar çek | RESOLVE |
| RESOLVE | Kazanan; para +/−bahis; el sonu sızıntı/regen | NEXT_HAND |
| NEXT_HAND | El++, min bahis++, süre güncelle | BET_CHECK |
| GAME_OVER | Bedel dökümü, best score, restart | INTRO |

**Akış özeti:** `BET_CHECK` para yetiyorsa `DEAL`'e, yetmiyor ama organ varsa `ORGAN_SALE`'e, hiç satılabilir organ yoksa (yalnız Kalp) `EXTRACTION → GAME_OVER`'a gider. Her el `RESOLVE`'da parayı takas eder, `NEXT_HAND` tabanı yükseltir, döngü `BET_CHECK`'e döner.

### Modüller
- **GameFlow:** State değişimleri, el/kademe, skor, yeniden başlatma.
- **Deck:** 52 kart (As çift değeri, resimli kartlar 10), karıştırma, çekme, en iyi toplamı hesaplama (yumuşak/sert el).
- **Dealer:** Sadece 17 kuralı (yumuşak 17'de durur); davranış ağacı veya öğrenen AI yok.
- **Bank:** Para, ayarlanabilir bahis (±$50, min↔para sınırlı), min bahis artışı, galibiyet/kayıp/beraberlik takası (ev payı), el sonu sızıntı/regen.
- **OrganManager:** Satılabilir/satılmış organlar, $ değerler, kur değiştiricileri (Dalak −$300 / Böbrek I +%20), satış doğrulama, Kalp fallback'i.
- **EffectStack:** Shader parametreleri, audio send'leri, süre değiştiricileri, el sonu para sızıntısı/regen, peek/hint ve galibiyet nakdi.
- **Presentation:** Kart/organ animasyon eventleri, ışık kademesi, altyazı ve kamera.
- **SaveData:** Yalnız best score, en yüksek el, en yüksek para, koşu sayısı ve ayarlar; run içi save/load yok.

### Data-Driven Organ Kaydı
> **ORGANDATA** — id · displayName · **dollarValue** · quantity · debuffType · debuffStrength · **buffType · buffStrength** · mesh · icon · extractionSFX · postProcessProfile. Değerlerin kod yerine data asset/resource üzerinden düzenlenmesi son saat dengelemesini hızlandırır.

**Örnek `buffType` enum'u:** `None, DealerCardHint, LowerCriticalThreshold, BetterSaleRate, SlowerBetRamp, ShowHandDistance, PeekHoleCard, ExtraDecisionTime, CashRegenOnWin`.
**Örnek `debuffType` enum'u:** `MonoAudio, WorseSaleRate, MinorVignette, CashBleedPerHand, NauseaOnHit, LeftEyeMask, ReducedDecisionTime, ColorShiftBleed, HeartEnd`.

---

## 09 / KAPSAM VE İÇERİK BÜTÇESİ

### MVP — Kesinlikle Olmalı
- Tam oynanabilir blackjack döngüsü: bahsi karşıla, çek/kal, kasa, sonuç, yeniden başla.
- **Para sistemi:** başlangıç $1.000, artan min bahis, galibiyet/kayıp/beraberlik takası.
- **Organ satışı:** para yetmeyince beden defteri, çoklu organ seçimi, Kalp fallback'i (soft-lock önleme).
- En az 5 işlevsel organ (debuff + buff): Kulak, Göz, Akciğer, Böbrek I–II ve Kalp.
- Tek ortak çıkarma animasyonu, organın masaya düşmesi ve kalıcı olarak görünmesi.
- En az üç kalıcı sunum etkisi: görme, nefes/süre, ses; kritik hâl katmanı.
- En az üç işlevsel buff: peek (Göz), iyi kur (Böbrek I) ve regen nakdi (Karaciğer) veya süre (Akciğer).

---

## 10 / ÜRETİM TAKVİMİ (48 SAAT)

| SAAT | HEDEF |
|---|---|
| 0 | 5 kararı kilitle; primitive'lerle blackjack döngüsü. |
| 12 | Çek/kal/kasa/sonuç çalışıyor; para + bahis takası + tek organ satışı. |
| 18 | Çoklu organ satışı, artan min bahis, debuff + buff iskeleti. Bir organ satışı parayı doldurmalı ve bir sonraki eli değiştirmelidir. |
| 30 | Placeholder ana modeller bitmeli; yeni temel mekanik eklenmemelidir. |
| 36 | Tüm takım aynı feature-complete build üzerinde çalışmalıdır. |
| 42 | İçerik kilidi; yalnız denge, okunabilirlik, ses seviyesi ve bug. |
| 46 | Release candidate başka bilgisayarda açılıp oynanmalıdır. |

### Paralel Görev Dağılımı
Programlama kart/state/para/organ sistemini; 3D-animasyon masa/el/organları; görsel-ses tarafı UI, shader, foley ve replikleri götürür. İki kişilik ekipte görsel-ses aynı kişi tarafından ardışık yapılır; kod tarafı asset beklemeden primitive'lerle ilerler.

---

## 11 / TEST, DENGE VE TAMAMLANMA

### İlk 10 Playtestte Ölçülecekler

| ALAN | SORU | KAYIT |
|---|---|---|
| Öğrenme | Yeni oyuncu 60 sn içinde Kart Çek/Kal ve bahsi açıklamasız anlıyor mu? | Evet / Hayır |
| Para fazı | İlk organ satışı kaçıncı elde geliyor? (Hedef: 2–4. el) | El no |
| Şanslı seri | Çok kazanan oyuncu organ fazını atlayabiliyor mu? | El no |
| Koşu süresi | İlk koşu 8–12 dk aralığında mı? | Dakika |
| Organ seçimi | Oyuncuların %70'inden fazlası aynı satış sırasını mı seçiyor? | Satış sırası |
| Buff dengesi | Buff'lar tek bir baskın organ sırası yaratıyor mu? | Satış sırası |
| Okunabilirlik | Göz/shader etkileri kart rakamını hâlâ okutuyor mu? | 1–5 |
| Rahatsızlık | Kamera/FOV bulantı yaratmadan baskı kuruyor mu? | 1–5 |
| Soft-lock | Para yetmediği her durumda satış ya da Kalp açılıyor mu? | 0 hata |
| Restart | Game over'dan yeni ele dönüş 5 sn altında mı? | Saniye |
| Performans | Hedef cihazda stabil 60 FPS; alt sınır 30 FPS mi? | FPS |

### Definition of Done
- Windows build temiz klasörde açılıyor; menüden oyuna ve oyundan menüye dönüyor.
- Arka arkaya en az 20 el boyunca state machine soft-lock üretmiyor.
- Tüm organlar yalnız bir kez satılabiliyor; satılmış state görsel ve sistemde aynı; buff'ları kalıcı uygulanıyor.
- Para $0'a inip satacak organ kalmayınca Kalp fallback'i ve 21'i aşma sonuçları doğru kapanıyor.
- Beraberlik ve kayıp parayı doğru eritiyor; galibiyet + blackjack 3:2 doğru ödüyor.
- Ses seviyeleri clipping yapmıyor; altyazı replikle senkron ve okunabilir.
- Shader kapatıldığında bile kartlar, para ve organ etkileri anlaşılır kalıyor.
- Sonuç ekranı "bedel dökümü"nü gösteriyor: el sayısı, toplam kaybedilen para, verilen organlar, best score.
- Itch sayfasında içerik uyarısı, kontroller, üç screenshot ve build talimatı var.

### Jam Başlamadan Kilitlenecek 5 Karar
1. Motor ve render pipeline
2. Ekip rol dağılımı ve dosya/version control yöntemi
3. 16:9 mu 4:3 mü; iç render çözünürlüğü
4. Full gore mu siluetli gore mu; içerik uyarısının dili
5. MVP organ sayısı: 5 mi 8 mi (her biri debuff + buff)

> **SON TASARIM İLKESİ** — Yeni bir fikir, mevcut çekirdek döngüyü güçlendirmiyorsa bu jam için iyi fikir değildir. Oyunun unutulacak kısmı 21'in kuralları; hatırlanacak kısmı bir sonraki eli görebilmek için **neyi bozdurduğun** ve karşılığında ne kazandığındır.
