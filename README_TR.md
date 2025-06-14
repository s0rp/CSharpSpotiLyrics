# CSharpSpotiLyrics

Spotify'dan şarkı sözlerini indirip `.lrc` dosyaları olarak kaydetmek için C# ile oluşturulmuş bir komut satırı aracıdır. Bu araç, tek tek parçalar, albümler, çalma listeleri, o anda çalan şarkınız, kütüphanenizdeki öğeler için şarkı sözlerini getirebilir veya hatta yerel ses dosyalarınız için meta verilerine göre şarkı sözlerini bulmaya çalışabilir. (DLL ile birlikte gelir, yani kendi kodunuzda kullanabilirsiniz :).)

---

⚠️ **Yasal Uyarı** ⚠️

**Bu proje Spotify'ın Hizmet Şartları'nı ihlal ediyor olabilir. Sorumluluk size ait olmak üzere ve riskleri göz önünde bulundurarak kullanın. Geliştiriciler, kullanımından kaynaklanabilecek herhangi bir sonuç için sorumluluk kabul etmez.**

---

## Özellikler

*   Spotify parçalarının, albümlerinin veya çalma listelerinin URL'lerini veya ID'lerini kullanarak şarkı sözlerini indirme.
*   Belirtilen bir dizindeki yerel ses dosyaları için meta verilerini okuyarak ve Spotify'da aratarak şarkı sözlerini getirme.
*   Spotify hesabınızda o anda çalan şarkının sözlerini indirme.
*   Spotify kütüphanenizde kayıtlı albümler veya çalma listeleri için interaktif olarak seçim yapıp şarkı sözlerini indirme.
*   Şarkı sözlerini standart `.lrc` formatında (senkronize şarkı sözleri) kaydetme.
*   Kalıcı ayarlar (indirme yolu, `sp_dc` token) için yapılandırma dosyası (`config.json`).
*   Yapılandırma ayarlarını geçersiz kılmak için komut satırı seçenekleri (indirme yolu, zorla üzerine yazma).
*   İnteraktif yapılandırma yönetimi (düzenle, sıfırla, yapılandırma dosyası konumunu aç).
*   Spotify `sp_dc` çerezinizi kullanarak kimlik doğrulama.
*   Şarkı sözleri bulunamayan veya indirilemeyen parçaları raporlama.

## Ön Gereksinimler

*   **.NET SDK:** Projeyi derlemek ve çalıştırmak için .NET SDK'nın kurulu olması gerekir (örneğin, .NET 6.0 veya üstü önerilir). [Buradan](https://dotnet.microsoft.com/download) indirin.
*   **Spotify `sp_dc` Çerezi:** Uygulama, kimlik doğrulama için Spotify web oturumunuzdan alınmış geçerli bir `sp_dc` çerezi gerektirir.

## Kurulum / Ayarlama

1.  **Depoyu Klonlayın:**
    ```bash
    git clone https://github.com/s0rp/CSharpSpotiLyrics
    cd CSharpSpotiLyrics
    ```
2.  **Projeyi Derleyin (İsteğe bağlı ancak önerilir):**
    ```bash
    dotnet build -c Release
    ```
    Bu, kodu derler. Doğrudan `dotnet run` kullanarak çalıştırabilir veya bağımsız bir yürütülebilir dosya için yayınlayabilirsiniz. (Cli dizinine `cd` yapmayı unutmayın!)

## Yapılandırma

Uygulamayı kullanmadan önce Spotify `sp_dc` çerezinizi **yapılandırmanız gerekir**.

**1. `sp_dc` Çerezinizi Nasıl Alırsınız:**

*   Web tarayıcınızı açın ve [open.spotify.com](https://open.spotify.com) adresine giriş yapın.
*   Tarayıcınızın Geliştirici Araçları'nı açın (genellikle `F12` tuşuna basarak).
*   "Uygulama" (Chrome/Edge) veya "Depolama" (Firefox) sekmesine gidin.
*   Kenar çubuğunda "Çerezler"i bulun ve `https://open.spotify.com` seçeneğini seçin.
*   `sp_dc` adlı çerezi bulun.
*   **Değerini** kopyalayın. Bu sizin token'ınızdır.

    **Güvenlik Notu:** `sp_dc` token'ınızı güvende tutun. Spotify hesabınıza erişim sağladığı için kimseyle paylaşmayın.

**2. `sp_dc` Çerezini Uygulamada Ayarlama:**

*   Uygulamayı ilk kez `edit` yapılandırma eylemiyle çalıştırın:
    ```bash
    # Proje dizininden
    dotnet run -- --config edit
    ```
    Veya yayınlanmış bir yürütülebilir dosyanız varsa (örneğin, `CSharpSpotiLyrics.exe` veya `CSharpSpotiLyrics`):
    ```bash
    ./CSharpSpotiLyrics --config edit
    ```
*   Uygulama, yapılandırma dosyasını (`config.json`) oluşturma/düzenleme konusunda size yol gösterecektir.
*   İstendiğinde kopyaladığınız `sp_dc` token'ını yapıştırın.
*   İstediğiniz varsayılan indirme yolunu ayarlayın.
*   Gerekirse `ForceDownload` gibi diğer seçenekleri yapılandırın.

Yapılandırma dosyası genellikle platforma özgü uygulama veri klasöründe saklanır. Uygulama, ilk çalıştırdığınızda veya düzenleme yaparken yolu gösterecektir.

**Diğer Yapılandırma Eylemleri:**

*   `--config reset`: Yapılandırmayı varsayılan değerlere sıfırlar (`sp_dc` token'ını tekrar girmeniz gerekir).
*   `--config open`: `config.json` dosyasını içeren dizini dosya gezgininizde açmaya çalışır.

## Kullanım

Uygulamayı, proje dizinindeki terminalinizden `dotnet run --` komutunu ve ardından argümanları ve seçenekleri kullanarak çalıştırın veya doğrudan yayınlanmış yürütülebilir dosyayı çalıştırın.

**Temel Sözdizimi:**

```bash
# dotnet run kullanarak
dotnet run -- [seçenekler] [<url_veya_yol>]

# Yayınlanmış yürütülebilir dosya kullanarak (örnek)
./CSharpSpotiLyrics [seçenekler] [<url_veya_yol>]
```

**Argümanlar:**

*   `url_veya_yol` (İsteğe bağlı): Spotify URL/ID'si (parça, albüm, çalma listesi) veya ses dosyalarını içeren yerel bir dizinin yolu.

**Seçenekler:**

*   `-d`, `--directory <yol>`: Bu çalıştırma için yapılandırmayı geçersiz kılarak bir indirme dizini belirtin.
*   `-f`, `--force`: `.lrc` dosyaları zaten mevcut olsa bile indirmeyi zorla. Yapılandırma ayarını geçersiz kılar.
*   `-c`, `--config <eylem>`: Yapılandırmayı yönetin (`edit`, `reset`, `open`).
*   `-u`, `--user <öğe>`: Giriş yapmış kullanıcının kütüphanesiyle etkileşim kurun (`current`, `album`, `play`).

**Örnekler:**

*   **Belirli bir parça URL'si için şarkı sözlerini indirme:**
    ```bash
    dotnet run -- "https://open.spotify.com/track/parca_id_niz"
    ```
*   **Bir albüm ID'si için şarkı sözlerini indirme:**
    ```bash
    dotnet run -- spotify:album:album_id_niz
    ```
*   **Bir çalma listesi URL'si için şarkı sözlerini indirme:**
    ```bash
    dotnet run -- "https://open.spotify.com/playlist/calma_listesi_id_niz"
    ```
*   **Bir dizindeki yerel dosyalar için şarkı sözlerini getirme:**
    ```bash
    dotnet run -- "/muzik/klasorunuzun/yolu"
    ```
*   **O anda çalan şarkının sözlerini indirme:**
    ```bash
    dotnet run -- --user current
    ```
*   **Kütüphanenizden bir albüm için şarkı sözlerini indirme (interaktif seçim):**
    ```bash
    dotnet run -- --user album
    ```
*   **Kütüphanenizden bir çalma listesi için şarkı sözlerini indirme (interaktif seçim):**
    ```bash
    dotnet run -- --user play
    ```
*   **Parça sözlerini indirirken indirme yolunu geçersiz kılma:**
    ```bash
    dotnet run -- --directory "/ozel/sarki_sozu/yolu" "spotify:track:parca_id_niz"
    ```
*   **Bir albüm için şarkı sözlerini zorla indirme:**
    ```bash
    dotnet run -- --force "spotify:album:album_id_niz"
    ```

## Katkıda Bulunanlar

*   **Geliştirme & C# Uygulaması:** S0rp
*   **Kodun Yeniden Yazılması & Düzenlenmesi:** Dixiz 3A
*   **Orijinal Konsept / Python Uygulaması :** [syrics by akashrchandran](https://github.com/akashrchandran/syrics)
