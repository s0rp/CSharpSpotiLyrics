# CSharpSpotiLyrics

Spotify'dan senkronize şarkı sözlerini (`.lrc` dosyaları) indirmek için C# ile geliştirilmiş profesyonel bir komut satırı aracıdır (CLI).

Bu araç; tekil parçalar, albümler, çalma listeleri, o anda aktif olarak çalan şarkınız veya doğrudan kayıtlı kütüphanenizden interaktif olarak seçeceğiniz içerikler için şarkı sözlerini indirebilir. Ayrıca yerel müzik klasörünüzdeki ses dosyalarını tarayıp meta verilerine göre Spotify üzerinde otomatik eşleştirme yaparak şarkı sözü indirebilme özelliğine de sahiptir.

---
### Alternatif Diller (README)
* [English (İngilizce)](https://github.com/s0rp/CSharpSpotiLyrics/blob/main/README.md)
---

> ⚠️ **Yasal Uyarı**  
> **Bu proje eğitim amaçlı geliştirilmiştir. Spotify'ın dahili API'lerine bu şekilde erişmek Hizmet Şartları'nı ihlal edebilir. Sorumluluk tamamen size ait olmak üzere kullanın. Geliştiriciler, kullanımından kaynaklanabilecek herhangi bir hesap kısıtlaması veya diğer sonuçlar için hiçbir sorumluluk kabul etmez.**

---

## Özellikler

*   **Çoklu Hedef Desteği:** Spotify Parça, Albüm veya Çalma Listesi URL'lerini ve benzersiz ID'lerini kullanarak senkronize şarkı sözü indirme.
*   **Yerel Dizin Eşleştirme:** Yerel ses dosyalarınızı tarayıp meta verilerini (ID3 etiketlerini) okur, Spotify üzerinde aratır ve `.lrc` dosyalarını doğrudan müzik dosyalarınızın yanına kaydeder.
*   **Aktif Oturum Senkronizasyonu:** Spotify hesabınızda o anda çalan aktif şarkının sözlerini anında indirebilme.
*   **İnteraktif Kütüphane Modu:** Spotify hesabınızda kayıtlı çalma listeleri veya albümler arasından terminal üzerinden interaktif seçim yaparak indirme.
*   **Standart LRC Çıktısı:** Çoğu modern medya oynatıcıyla uyumlu, standart senkronize şarkı sözü (`.lrc`) çıktısı sağlar.
*   **Sıfır Kurulum Tarayıcı Desteği:** Dahili Playwright altyapısı, çalışma zamanında (runtime) gereken Chromium ortamını otomatik olarak kurar; manuel olarak tarayıcı kurmanız gerekmez.
*   **Esnek Yapılandırma:** Kalıcı ayarlarınız (varsayılan indirme yolu, `sp_dc` çerezi) için `config.json` dosyası kullanılır. Komut satırı seçenekleri ile bu ayarlar çalışma anında geçici olarak değiştirilebilir.
*   **Önbellek Yönetimi ve Sorun Giderme:** Bağlantı veya senkronizasyon hatası almanız durumunda TOTP anahtarlarını ve dinamik GraphQL hash dosyalarını tek bir komutla temizleme olanağı sunar.

---

## Ön Gereksinimler

*   **.NET SDK:** Kaynak kodunu derlemek ve çalıştırmak için .NET SDK (sürüm 6.0 veya daha yeni bir sürüm önerilir) gereklidir. [.NET SDK İndir](https://dotnet.microsoft.com/download).
*   **Spotify `sp_dc` Çerezi:** API isteklerinin doğrulanması için Spotify web oturumunuzdan alınmış geçerli bir `sp_dc` oturum çerezi gerekir.

---

## Kurulum ve Ayarlama

1. **Depoyu Klonlayın:**
   ```bash
   git clone https://github.com/s0rp/CSharpSpotiLyrics
   cd CSharpSpotiLyrics/Cli
   ```

2. **Projeyi Derleyin:**
   ```bash
   dotnet build -c Release
   ```

Uygulamayı doğrudan .NET CLI kullanarak çalıştırabilir veya `/bin/Release/` dizini altında derlenen bağımsız binary dosyasını doğrudan terminalinizde yürütebilirsiniz.

---

## Yapılandırma

Şarkı sözlerini indirmeye başlamadan önce Spotify `sp_dc` web çerezinizi uygulamaya kaydederek kimlik doğrulaması yapmanız **gerekir**.

### 1. `sp_dc` çerezinizi nasıl alırsınız?
1. Web tarayıcınızı açın ve [open.spotify.com](https://open.spotify.com) adresine giriş yapın.
2. Tarayıcınızın Geliştirici Araçları'nı açın (genellikle `F12` veya `Sağ Tık -> İncele` ile).
3. **Uygulama (Application)** (Chrome/Edge) veya **Depolama (Storage)** (Firefox) sekmesine gidin.
4. Soldaki menüden **Çerezler (Cookies)** seçeneğini genişletin ve `https://open.spotify.com` adresini seçin.
5. Listeden `sp_dc` isimli çerezi bulun ve karşısındaki harf-rakam karışımı **Değer (Value)** alanını kopyalayın.

> **Güvenlik Uyarısı:** `sp_dc` çereziniz hesabınıza doğrudan erişim sağlar. Güvenliğini sağlayın ve asla başkalarıyla paylaşmayın.

### 2. Yapılandırma dosyasını ayarlama:
İnteraktif yapılandırma aracını çalıştırın:
```bash
dotnet run -- --config edit
```
Bu arayüz sizi şu adımlarda yönlendirecektir:
* Kopyaladığınız `sp_dc` değerini yapıştırma.
* Şarkı sözlerinin kaydedileceği varsayılan indirme dizinini belirtme.
* İndirme tercihlerinizi (örneğin dosya varsa üzerine yazılsın mı) yapılandırma.

*Not: Yapılandırma klasörü işletim sisteminize göre değişiklik gösterebilir. CLI aracı, ilk çalıştırmada veya düzenleme yaparken `config.json` dosyanızın tam konumunu size gösterecektir.*

---

## Kullanım

```bash
# dotnet run kullanarak
dotnet run -- [seçenekler] [<url_veya_yol>]

# Derlenmiş binary dosyasını doğrudan çalıştırarak
./CSharpSpotiLyrics [seçenekler] [<url_veya_yol>]
```

### Argümanlar
*   `<url_veya_yol>`: *(İsteğe bağlı)* Spotify parça/albüm/çalma listesi URL'si/ID'si veya yerel müzik klasörünüzün yolu.

### Seçenekler

| Seçenek / Komut | Açıklama |
| :--- | :--- |
| `-d`, `--directory <yol>` | Bu çalıştırma için yapılandırmadaki varsayılan indirme klasörünü geçici olarak geçersiz kılar. |
| `-f`, `--force` | Klasörde `.lrc` dosyası zaten mevcut olsa bile indirmeyi zorlar (üzerine yazar). |
| `-cl`, `--clearcache` | Senkronizasyon ve bağlantı hatalarını gidermek için yerel önbellekleri (`.SPOTIFYTOTP` ve `.SPOTIFYHASH`) temizler. |
| `-u`, `--user <öğe>` | Giriş yapmış kullanıcının kütüphanesiyle etkileşime girer. Değerler: `current`, `album`, `play`. |
| `-c`, `--config <eylem>` | Yapılandırma yardımcısını başlatır. Değerler: `edit`, `reset`, `open`. |

---

## Örnek Komutlar

### Spotify Bağlantısı veya ID ile İndirme
```bash
# Parça URL'si ile
dotnet run -- "https://open.spotify.com/track/4PTG3Z6ehGkBFmYskgR96g"

# Albüm ID'si ile (URI Formatı)
dotnet run -- "spotify:album:29D78864XbAUp6v"

# Çalma Listesi Bağlantısı ile (içindeki tüm şarkıları indirir)
dotnet run -- "https://open.spotify.com/playlist/37i9dQZF1DXcBWIGg6cmY8"
```

### Yerel Klasör Taraması ve Eşleştirme
```bash
# Klasördeki müzik dosyalarının meta verilerini tarayıp otomatik olarak sözleri indirir
dotnet run -- "/home/kullanici/Muzik/MuzikKlasorüm"
```

### Aktif Oturum ve Kütüphane Etkileşimi
```bash
# Spotify'da o anda aktif olarak çalan şarkınızın sözlerini indirir
dotnet run -- --user current

# Kütüphanenizde kayıtlı çalma listelerini listeleyip interaktif seçerek indirir
dotnet run -- --user play

# Kütüphanenizde kayıtlı albümleri listeleyip interaktif seçerek indirir
dotnet run -- --user album
```

### Yapılandırma ve Sorun Giderme
```bash
# Şarkı sözleri klasörde mevcut olsa bile indirmeyi zorla
dotnet run -- --force "https://open.spotify.com/track/4PTG3Z6ehGkBFmYskgR96g"

# Önbellekteki geçici anahtarları sıfırlayarak olası "Bad Request" hatalarını temizle
dotnet run -- --clearcache "https://open.spotify.com/track/4PTG3Z6ehGkBFmYskgR96g"

# config.json dosyasının bulunduğu klasörü Dosya Gezgini'nde aç
dotnet run -- --config open
```

---

## Sorun Giderme

Eğer istemci başlatılırken kimlik doğrulama veya bağlantı hataları (örneğin `400 Bad Request`) alırsanız:
1. `sp_dc` çerezinizin süresinin dolmadığından emin olun. Tarayıcınızdan Spotify Web Oynatıcısına girdiğinizde hala oturumunuzun açık olduğunu doğrulayarak bunu test edebilirsiniz.
2. Uygulamayı `-cl` / `--clearcache` seçeneğiyle çalıştırın. Bu işlem yerel geçici token'ları ve dynamic hash dosyalarını temizler; Playwright'ın Spotify ile bağlantıyı en baştan kurarak dynamic GraphQL hash'lerini otomatik olarak yenilemesini sağlar.

---

## Katkıda Bulunanlar


*   **Geliştirme & C# Çekirdek Mimarisi:** s0rp