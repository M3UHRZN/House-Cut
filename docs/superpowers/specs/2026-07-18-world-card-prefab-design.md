# Issue 60: Masa kartı prefab tasarımı

## Amaç

Blackjack kartlarını geçici küp ve `TextMesh` görünümünden çıkarıp masaya yerleştirilebilen tek bir prefab üzerinden göstermek. Oyuncu ve krupiye kartlarının konumu, sahnedeki boş `Transform` slotlarıyla Inspector'dan ayarlanabilmeli.

## Kapsam

- Tek bir `Card.prefab` bütün 52 kartı ve kapalı kartı gösterecek.
- İndirilen uVegas paketindeki Classic kart görselleri kullanılacak.
- Kartlar dünya uzayında yer alacak; ekran Canvas'ına bağlı olmayacak.
- Oyuncu ve krupiye dağıtım noktaları Table sahnesine eklenecek.
- Mevcut `BlackjackLoop`, deste ve ekonomi kuralları değişmeyecek.
- Kart dağıtma animasyonu, ses, hover ve kart seçimi bu işin dışında kalacak.

## Asset kullanımı

uVegas paketi 52 hazır yüz yerine şu katmanları sağlıyor:

- 13 rank sprite'ı: As, 2-10, Vale, Kız ve Papaz
- 4 suit sprite'ı: Sinek, Karo, Kupa ve Maça
- ön yüz ve arka yüz sprite'ları

Paketin `Prefabs/UI/Mobile/Card.prefab` dosyası UGUI için hazırlanmış. Masa kartı bu prefabı kullanmayacak. Yalnızca sprite dosyaları alınacak; böylece `Game` assembly'si uVegas koduna bağımlı olmayacak.

## Görsel yön

Kartların adı verilen görünümü "kirli klasik deste"dir. Classic White tabanı kullanılacak. Ön yüz kirli kemik rengine yakın, kupa ve karo oksit kırmızısı, sinek ve maça kömür siyahı olacak. Arka yüz koyu ve masanın geri kalanından kolay seçilir kalacak.

Prefab dünya uzayında 0,7 x 1 birim ölçüsünde, 5:7 kart oranını koruyacak. `Body` kalınlığı 0,02 birim olacak. Ön yüz, rank, suit ve arka yüz sprite'ları gövdenin üzerinde ayrı katmanlarda duracak.

## Prefab yapısı

```text
Card
|- Body
|- Front
|- Rank
|- Suit
`- Back
```

- `Card` kökünde `CardVisual` bulunacak.
- `Body`, çok ince bir kart gövdesi olacak.
- `Front`, `Rank`, `Suit` ve `Back` çocuklarında `SpriteRenderer` kullanılacak.
- Ön yüz açıkken `Front`, `Rank` ve `Suit` görünür; `Back` kapanır.
- Kapalı kartta yalnızca `Back` görünür.
- Sprite katmanlarına sabit sorting order değerleri verilecek: `Front` 0, `Rank` 1, `Suit` 2 ve `Back` 3. Ön ve arka yüz grupları aynı anda açık olmayacağı için z-fighting oluşmayacak.

## CardVisual sorumluluğu

`CardVisual.SetCard(Card card, bool faceUp)` mevcut çağrı noktası olarak kalacak.

`CardVisual` şunları tutacak:

- ön ve arka yüz renderer referansları
- rank ve suit renderer referansları
- `Game.Cards.Rank` sırasıyla eşleşen 13 sprite
- `Game.Cards.Suit` sırasıyla eşleşen 4 sprite
- kırmızı ve siyah suit renkleri

Enum eşlemesi açık yazılacak. Dizi sınırı veya eksik sprite yüzünden oyun durmayacak; kart güvenli bir placeholder görünümüne düşecek ve açıklayıcı bir uyarı yazacak.

## Masaya yerleştirme

Table sahnesine şu hiyerarşi eklenecek:

```text
CardSlots
|- PlayerSlots
|  |- PlayerSlot_0
|  |- PlayerSlot_1
|  `- ...
`- DealerSlots
   |- DealerSlot_0
   |- DealerSlot_1
   `- ...
```

Slotlar yalnızca boş `Transform` objeleri olacak. Her slotun dünya pozisyonu ve rotasyonu, kartın masada nerede ve hangi açıyla duracağını belirleyecek. Kullanıcı slotları Scene view içinde taşıyarak yerleşimi değiştirebilecek.

`BlackjackLoopRunner` üzerinde:

- `cardPrefab`, yeni `Card.prefab` dosyasına bağlanacak.
- `playerSlots`, `PlayerSlot_*` objelerine bağlanacak.
- `dealerSlots`, `DealerSlot_*` objelerine bağlanacak.
- `cardRoot`, sahnedeki kartları düzenli tutan bir kök objeye bağlanacak.

Slot sayısı aşılırsa mevcut yedek sıra yerleşimi çalışmaya devam edecek. İlk el ve birkaç Hit için her tarafta en az altı slot hazırlanacak.

## Veri akışı

1. `BlackjackLoop` bir kart dağıtır ve `CardDealt` olayını yayınlar.
2. `BlackjackLoopRunner` `Card.prefab` örneği oluşturur.
3. Oluşan kart ilgili oyuncu veya krupiye slotuna taşınır.
4. `CardVisual.SetCard` rank, suit ve açık/kapalı bilgisini sprite katmanlarına uygular.
5. Krupiyenin kapalı kartı açıldığında aynı prefab `SetCard(card, true)` ile güncellenir.
6. El bittiğinde mevcut `ClearCards` akışı oluşturulan kartları siler.

## Hata davranışı

- Prefab atanmamışsa geliştirme sırasında sahnenin çalışabilmesi için mevcut primitive fallback korunacak ve uyarı yazılacak. Kaydedilmiş Table sahnesinde prefab atanmış olacak; normal oyun akışı fallback kullanmayacak.
- Slot atanmamışsa yedek sıra yerleşimi kullanılacak.
- `CardVisual` referanslarından biri eksikse NullReference üretmek yerine uyarı verecek.
- Rank veya suit sprite'ı eksikse kart gövdesi görünür kalacak.

## Doğrulama

- Script değişikliklerinden sonra Unity derlemesi hatasız tamamlanmalı.
- EditMode testleri mevcut blackjack davranışında regresyon olmadığını göstermeli.
- Play Mode'da ilk dağıtımda iki oyuncu ve iki krupiye kartı doğru slotlara gelmeli.
- Krupiyenin ikinci kartı önce arka yüzü, sonra doğru ön yüzü göstermeli.
- Yukarı/aşağı ok ve Enter akışı kart prefab değişikliğinden sonra çalışmaya devam etmeli.
- Bir slot Scene view içinde taşındığında yeni dağıtılan kart yeni konuma gelmeli.

## Kabul ölçütleri

- Kaydedilmiş Table sahnesindeki normal oyun akışı geçici küp ve `TextMesh` kart üretmiyor.
- Bütün rank ve suit birleşimleri tek prefab ile gösterilebiliyor.
- Kapalı krupiye kartı arka yüzle görünüyor ve doğru zamanda açılıyor.
- Kart yerleşimi kod değiştirmeden Inspector ve Scene view üzerinden ayarlanabiliyor.
- Table sahnesi kaydedilmiş ve Console'da yeni hata bulunmuyor.
