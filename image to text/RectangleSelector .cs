using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;


/// <summary>
/// Kullanıcının bir PictureBox üzerinde dikdörtgen seçim yapmasını sağlayan sınıftır.
/// Seçim yapılan alanı çizmek, yönetmek ve istenirse bir görüntüden kırpmak için kullanılır.
/// </summary>
public class RectangleSelector
{
    // Anlık olarak seçim yapılıp yapılmadığını kontrol etmek için boolean değişkeni
    private bool isSelecting = false;

    // Görüntü üzerinde seçime başlanan nokta (x,y) biçiminde saklanır
    private Point startPoint;

    /// <summary>
    /// Seçilen alanın dikdörtgen nesnesidir. X, Y, genişlik ve yükseklik bilgilerini içerir.
    /// </summary>
    public Rectangle SelectionRectangle { get; set; } = Rectangle.Empty;

    // Kullanıcının seçim yaptığı Control (örneğin PictureBox üzerinde işlem yaptığım için bu kutuyu)
    private Control targetControl;

    /// <summary>
    /// Yapıcı metot, RectangleSelector sınıfının bir örneğini oluşturur ve gerekli atamaları yapar.
    /// </summary>
    /// <param name="control">Seçim yapılacak hedef Control (örneğin PictureBox).</param>
    public RectangleSelector(Control control)
    {
        this.targetControl = control;

        // Mouse olaylarını dinler. Anlık olarak PictureBox üzerinde yapılan olayları günceller.
        control.MouseDown += Control_MouseDown; // Mouse'a basıldığında çalışır
        control.MouseMove += Control_MouseMove; // Mouse hareket ettiğinde çalışır
        control.MouseUp += Control_MouseUp; // Mouse bırakıldığında çalışır
        control.Paint += Control_Paint; // PictureBox tekrar çizildiğinde çalışır
    }

    /// <summary>
    /// Mouse'a basıldığında çalışan olay.
    /// Seçim başlangıç noktasını belirler ve önceki dikdörtgeni sıfırlar.
    /// </summary>
    private void Control_MouseDown(object sender, MouseEventArgs e)
    {
        isSelecting = true;
        // Seçim başlangıç noktasını kaydet
        startPoint = new Point(e.Location.X, e.Location.Y);

        // Önceki seçim dikdörtgenini sıfırla
        SelectionRectangle = Rectangle.Empty;
    }

    /// <summary>
    /// Mouse hareket edince çalışan olay.
    /// Seçim işlemi devam ediyorsa seçim dikdörtgenini oluşturur ve kontrolü yeniden çizer.
    /// </summary>
    private void Control_MouseMove(object sender, MouseEventArgs e)
    {
        // Mouse basılı tuşla hareket ediliyorsa seçim işlemi devam ediyor demektir
        if (isSelecting)
        {
            // Seçilen dikdörtgenin boyutlarını hesapla
            SelectionRectangle = new Rectangle(
                Math.Min(startPoint.X, e.X), // Sol x koordinatı
                Math.Min(startPoint.Y, e.Y), // Üst y koordinatı
                Math.Abs(e.X - startPoint.X), // Genişlik
                Math.Abs(e.Y - startPoint.Y) // Yükseklik
            );
            targetControl.Invalidate(); // PictureBox'ı yeniden çizerek görüntüyü günceller (burada event'i tetikliyor)
        }
    }

    /// <summary>
    /// Mouse bırakıldığında çalışan olay.
    /// Seçim işlemini bitirir ve kontrolü yeniden çizer.
    /// </summary>
    private void Control_MouseUp(object sender, MouseEventArgs e)
    {
        isSelecting = false; // Seçim işlemi bittiğinde false yapar
        targetControl.Invalidate(); // İşlem bittiğinde görsel üzerinde çizim yaptığım son seçim dikdörtgenini günceller
    }

    /// <summary>
    /// PictureBox yeniden çizildiğinde çalışan olay.
    /// Seçim yapılmışsa ve geçerliyse dikdörtgeni çizer.
    /// </summary>
    private void Control_Paint(object sender, PaintEventArgs e)
    {
        // Seçim varsa ve boyutları sıfırdan büyükse
        if (SelectionRectangle.Width > 0 && SelectionRectangle.Height > 0)
        {
            using (Pen pen = new Pen(Color.Red, 2)) // Kırmızı, 2 px kalınlığında kalem tanımlanıyor
            {
                e.Graphics.DrawRectangle(pen, SelectionRectangle); // Görüntü üzerinde seçim dikdörtgenini çiziliyor
            }
        }
    }

    /// <summary>
    /// Seçim alanını sıfırlayan yardımcı metot.
    /// </summary>
    public void Reset()
    {
        SelectionRectangle = Rectangle.Empty;
        targetControl.Invalidate();
    }

    /// <summary>
    /// Seçilen alanı verilen OpenCV Mat görüntüden kırpar.
    /// Görüntü PictureBox'ta ölçeklendirilmiş ve ortalanmışsa, doğru koordinatları hesaplar.
    /// </summary>
    /// <param name="img">Kırpılacak Mat türündeki OpenCV görüntüsü.</param>
    /// <returns>
    /// Seçilen alanı içeren yeni Mat nesnesi. 
    /// Seçim yapılmamışsa veya geçersizse <c>null</c> döner.
    /// </returns>
    public Mat Crop(Mat img)
    {
        // Eğer kişi seçim yapmamışsa
        if (img == null || SelectionRectangle.Width <= 0 || SelectionRectangle.Height <= 0)
            return null;

        // Orijinal resmin boyutlarını al
        int imgW = img.Width;
        int imgH = img.Height;

        // PictureBox boyutları
        int pbW = targetControl.Width;
        int pbH = targetControl.Height;

        // Zoom modunda görüntü, PictureBox'a orantılı şekilde sığdırılır.
        // Bu yüzden hem X hem de Y oranını alıp en küçüğünü kullanıyoruz.
        float ratioX = (float)pbW / imgW; // Genişlik oranı
        float ratioY = (float)pbH / imgH; // Yükseklik oranı
        float ratio = Math.Min(ratioX, ratioY); // En küçük oran seçilir (Zoom'a göre boyutlandırılmış hali)

        // Görüntünün PictureBox içindeki ekranda gösterilen boyutlarını hesapla
        int displayedImgW = (int)(imgW * ratio);
        int displayedImgH = (int)(imgH * ratio);

        // Zoom modunda resim ortalandığı için offset (sol-üst boşluk) hesaplanır
        int offsetX = (pbW - displayedImgW) / 2;
        int offsetY = (pbH - displayedImgH) / 2;

        // Seçim dikdörtgeninin PictureBox üzerindeki konumunu, gerçek resmin koordinat sistemine dönüştür
        // Yani ekrandaki seçim karesinin, resimdeki karşılığı nedir, onu buluyoruz
        int x = (int)((SelectionRectangle.X - offsetX) / ratio); // Gerçek resimdeki sol kenar
        int y = (int)((SelectionRectangle.Y - offsetY) / ratio);  // Gerçek resimdeki üst kenar
        int width = (int)(SelectionRectangle.Width / ratio); // Gerçek genişlik
        int height = (int)(SelectionRectangle.Height / ratio);  // Gerçek yükseklik

        // Koordinatlar resim sınırları dışına taşmış olabilir, onları sınırlar içine sabitle
        x = Math.Max(0, x);
        y = Math.Max(0, y);
        width = Math.Min(width, imgW - x);
        height = Math.Min(height, imgH - y);

        // Eğer kırpılacak alan geçersizse (negatif boyut vs.) işlemi iptal et
        if (width <= 0 || height <= 0)
            return null;

        // Seçilen alanı (ROI) tanımla
        Rect roi = new Rect(x, y, width, height);

        Reset(); // Seçimi sıfırla, böylece bir sonraki seçim için hazır olur

        // Dışardan alınan görüntüden seçilen alanı kırp ve yeni görüntüyü döndür.
        return new Mat(img, roi);
    }
}
