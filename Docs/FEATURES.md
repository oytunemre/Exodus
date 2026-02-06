# Exodus - A'dan Z'ye Proje Ozellikleri

**ASP.NET Core 10.0** ile gelistirilmis kapsamli bir e-ticaret marketplace platformu.

---

## A - Authentication (Kimlik Dogrulama)
- JWT tabanli token authentication
- Refresh token yonetimi
- Hesap kilitleme (basarisiz giris denemeleri sonrasi)

## B - Brand Management (Marka Yonetimi)
- Urunlere marka atama
- Admin panelinden marka CRUD islemleri

## C - Campaign & Coupon (Kampanya ve Kupon Sistemi)
- 6 farkli kampanya tipi: Yuzde indirim, sabit tutar, X al Y bedava, X al Y ode, ucretsiz kargo, minimum tutar
- Kupon kodu olusturma ve takibi
- Kampanya oncelik sirasi ve ust uste binme (stackable) destegi
- Kullanici basina kullanim limiti

## D - Dashboard (Kontrol Paneli)
- Admin dashboard: Satis istatistikleri, analitikler
- Seller dashboard: Saticiya ozel siparis ve performans verileri

## E - Email Service (E-posta Servisi)
- SMTP uzerinden e-posta gonderimi
- E-posta dogrulama (email verification)
- Sifre sifirlama e-postalari
- Dinamik e-posta sablonlari (Email Templates)

## F - File Upload (Dosya Yukleme)
- Urun gorselleri yukleme
- Avatar yukleme/silme
- Destek talebi ekleri
- Dosya boyutu ve uzanti kontrolu
- Thumbnail olusturma

## G - Gift Card (Hediye Karti)
- Hediye karti olusturma ve yonetimi
- Bakiye takibi
- Son kullanma tarihi
- Kullanim gecmisi
- Kisisel mesaj destegi

## H - Health Check (Saglik Kontrolu)
- `/health` - Uygulama saglik durumu
- `/health/ready` - Veritabani hazirlik kontrolu
- Docker/Kubernetes deployment destegi

## I - Invoice & Iyzico (Fatura ve Odeme Entegrasyonu)
- Fatura olusturma ve numaralandirma
- Iyzico odeme gecidi entegrasyonu
- 3D Secure destegi
- Taksitli odeme (3, 6, 9, 12 ay)

## J - JWT Security (JWT Guvenligi)
- Token bazli oturum yonetimi
- Claims tabanli yetkilendirme
- Token yenileme mekanizmasi
- Simetrik anahtar sifreleme

## K - Kategori Yonetimi (Category Management)
- Hiyerarsik kategori yapisi (ana/alt kategoriler)
- Urun-kategori eslestirmesi
- CRUD islemleri

## L - Listing (Pazar Yeri Ilanlari)
- Satici urun ilanlari ve fiyatlandirma
- Stok yonetimi (dusuk stok esigi uyarisi)
- Ilan durumu: Yeni/Kullanilmis
- SKU yonetimi

## M - Marketplace Multi-Seller (Coklu Satici Pazaryeri)
- Tek sipariste birden fazla saticidan alisveris
- Otomatik siparis bolme (satici bazli)
- Satici komisyon yonetimi

## N - Notification (Bildirim Sistemi)
- Kullanici bildirimleri
- Bildirim tercihleri yonetimi
- Okundu/okunmadi durumu
- Tercih bazli filtreleme

## O - Order Management (Siparis Yonetimi)
- Sepetten siparis olusturma (checkout)
- 10+ siparis durumu: Pending, Processing, Confirmed, Shipped, Delivered, Completed, Cancelled, Refunded vb.
- Siparis zaman cizelgesi (order events)
- Siparis notlari (musteri ve admin)
- Iptal nedeni takibi
- Fatura ve kargo adresi snapshot

## P - Payment Processing (Odeme Isleme)
- 6 odeme yontemi: Kapida odeme, banka havalesi, kredi/banka karti, cuzdan, taksit, simdi al sonra ode
- Odeme yetkilendirme ve yakalama (authorize & capture)
- Tam ve kismi iade
- 3D Secure dogrulama akisi
- Odeme olay takibi

## R - Review & Rating (Degerlendirme ve Puanlama)
- 5 yildizli puanlama sistemi
- Urun ve satici degerlendirmeleri
- Dogrulanmis satin alma rozeti
- Degerlendirme faydalilik oylamasi
- Degerlendirme raporlama ve moderasyon
- Satici yanitlari
- Degerlendirme gorselleri

## S - Shipment & Support (Kargo ve Destek)

### Kargo
- Kargo takip numarasi yonetimi
- Kargo durumu takibi ve zaman cizelgesi
- Birden fazla kargo firmasi destegi

### Iade Kargo
- Iade kodu (RET-XXXXXXX-XXXX)
- Akilli kargo ucreti atama (alici/satici/platform oder)
- Iade inceleme is akisi

### Destek
- 8 kategori: Genel, Siparis, Kargo, Urun, Odeme, Iade, Satici, Teknik
- 4 oncelik seviyesi: Dusuk, Normal, Yuksek, Acil
- Dosya eki destegi
- Memnuniyet puanlama
- Dahili notlar

## T - Two-Factor Authentication (Iki Faktorlu Dogrulama)
- TOTP tabanli 2FA
- 2FA etkinlestirme/devre disi birakma
- Yedek kodlar olusturma
- Giris sirasinda 2FA dogrulama

## U - User Management (Kullanici Yonetimi)
- 3 rol: Customer, Seller, Admin
- Kullanici kaydi ve profil yonetimi
- Avatar yukleme
- Sifre degistirme
- Adres yonetimi (coklu adres)
- Kullanici istatistikleri

## V - Validation (Dogrulama)
- FluentValidation ile girdi dogrulama
- Domain bazli validator siniflari (Login, Register, Product, Listing, Cart, Payment, Shipment, User)
- Is kurali dogrulamalari

## W - Wishlist (Istek Listesi)
- Kullanici istek listeleri
- Urun ve ilan bazli favori ekleme/cikarma

## X - XSS Protection & Security (Guvenlik)
- Input sanitization (XSS onleme)
- Rate limiting: 3 farkli politika (100/dk genel, 10/dk auth, 5/dk hassas)
- BCrypt sifre hashleme
- HTTPS zorunlulugu (production)
- CORS politikasi

## Y - Yonetim Paneli (Admin Panel)
- 31 admin controller ile kapsamli yonetim
- Kullanici, siparis, odeme, kargo, urun, satici, kampanya yonetimi
- Icerik yonetimi: Banner, statik sayfa, ana sayfa widget
- Site ayarlari, vergi oranlari, bolge yonetimi
- Rapor olusturma: Satis, envanter, musteri raporlari
- Denetim kayitlari (audit logs)

## Z - Zone & Region Management (Bolge Yonetimi)
- Hiyerarsik bolge yapisi (ulke > sehir > ilce)
- Vergi orani bolge eslestirmesi
- Satici kargo bolgeleri

---

## Ek Ozellikler

| Ozellik | Detay |
|---|---|
| **Affiliate/Referral** | Ortaklik programi, referans kodu, komisyon takibi, UTM parametreleri |
| **Seller Verification** | Satici dogrulama is akisi (Pending -> UnderReview -> Approved/Rejected/Suspended) |
| **Seller Payout** | Satici odeme yonetimi, IBAN bilgileri |
| **Soft Delete** | Veri kaybini onleyen yumusak silme deseni |
| **Audit Logging** | Tum islemlerin denetim kaydi |
| **Database Seeding** | Test verisi ile veritabani doldurma |
| **Global Exception Handling** | Tutarli hata yanitlari icin middleware |

---

## Rakamlarla Proje

| Metrik | Deger |
|---|---|
| Entity Model | 45+ |
| DTO Sinifi | 100+ |
| API Controller | 49 (31 admin + 3 seller + 15 public) |
| Servis | 20+ |
| Enum Tipi | 30+ |
| Veritabani Migration | 23 |
| NuGet Paket | 6 ana bagimlilik |

---

## Teknoloji Yigini

- **Framework:** ASP.NET Core 10.0
- **Dil:** C#
- **ORM:** Entity Framework Core 10.0
- **Veritabani:** SQL Server
- **Authentication:** JWT Bearer + 2FA (TOTP)
- **Validation:** FluentValidation 11.3.1
- **Password Hashing:** BCrypt.Net
- **Odeme:** Iyzico Payment Gateway
- **E-posta:** SMTP (Gmail)
- **API Dokumantasyonu:** OpenAPI/Swagger
