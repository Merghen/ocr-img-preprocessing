using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace image_to_text
{
    /// <summary>
    /// Bu sınıf, bir resim üzerinde zoom (yakınlaştırma/uzaklaştırma) ve pan (sürükleme) işlemlerini yönetir.
    /// </summary>
    public class ImageZoomController
    {
        private PictureBox pictureBox;       // Zoom yapılacak görüntüyü tutan PictureBox
        private Panel scrollPanel;           // PictureBox'ın bulunduğu scroll destekli Panel

        private bool isPanning = false;      // Sürükleme (pan) işlemi aktif mi?
        private Point mouseDownLocation;     // Mouse'a basıldığı anda konumu

        private float zoomFactor = 1.0f;     // Resmin şu anki zoom oranı (1.0 = %100)
        private const float zoomStep = 1.1f; // Her adımda büyüme/küçülme oranı
        private const float minZoom = 0.1f;  // Minimum zoom seviyesi (%10)
        private const float maxZoom = 10f;   // Maksimum zoom seviyesi (%1000)

        /// <summary>
        /// Yapıcı (Constructor) metot: ImageZoomController sınıfını başlatır ve gerekli olaylara abone olur.
        /// </summary>
        /// <param name="pictureBox">Zoom yapılacak PictureBox nesnesi.</param>
        /// <param name="scrollPanel">PictureBox'ın içinde bulunduğu ve kaydırma destekli Panel.</param>
        public ImageZoomController(PictureBox pictureBox, Panel scrollPanel)
        {
            this.pictureBox = pictureBox;
            this.scrollPanel = scrollPanel;

            // Zoom için mouse tekerleği olayına abone ol
            this.pictureBox.MouseWheel += PictureBox_MouseWheel;
            this.pictureBox.MouseDown += PictureBox_MouseDown;
            this.pictureBox.MouseMove += PictureBox_MouseMove;
            this.pictureBox.MouseUp += PictureBox_MouseUp;

            // PictureBox ayarlarını yap
            this.pictureBox.SizeMode = PictureBoxSizeMode.Normal; // Stretch ya da Zoom değil, kendimiz kontrol edeceğiz

            // Panel ayarları
            this.scrollPanel.AutoScroll = true; // Scroll bar'lar aktif olmalı
        }

        /// <summary>
        /// Fare tekerleği döndüğünde çalışan olay: resmi yakınlaştırır veya uzaklaştırır.
        /// Zoom işlemi sonrası scroll pozisyonunu günceller.
        /// </summary>
        /// <param name="sender">Olayı tetikleyen nesne.</param>
        /// <param name="e">MouseWheel olayı bilgisi.</param>
        private void PictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (pictureBox.Image == null) return;

            float oldZoom = zoomFactor;

            if (e.Delta > 0)
                zoomFactor *= zoomStep;
            else
                zoomFactor /= zoomStep;

            zoomFactor = Math.Max(minZoom, Math.Min(maxZoom, zoomFactor));

            int newWidth = (int)(pictureBox.Image.Width * zoomFactor);
            int newHeight = (int)(pictureBox.Image.Height * zoomFactor);

            float scale = zoomFactor / oldZoom;

            Point scrollPos = scrollPanel.AutoScrollPosition;
            scrollPos = new Point(-scrollPos.X, -scrollPos.Y);

            int offsetX = (int)((e.X + scrollPos.X) * scale - e.X);
            int offsetY = (int)((e.Y + scrollPos.Y) * scale - e.Y);

            pictureBox.Size = new Size(newWidth, newHeight);
            scrollPanel.AutoScrollPosition = new Point(offsetX, offsetY);
            pictureBox.Invalidate();
        }

        /// <summary>
        /// Fare sol tuşuna basıldığında çalışan olay: pan (sürükleme) işlemini başlatır.
        /// </summary>
        /// <param name="sender">Olayı tetikleyen nesne.</param>
        /// <param name="e">MouseDown olayı bilgisi.</param>
        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isPanning = true;
                mouseDownLocation = e.Location;
                pictureBox.Cursor = Cursors.Hand;
            }
        }

        /// <summary>
        /// Fare hareket ettiğinde çalışan olay: pan işlemi aktifse resmi sürükler.
        /// </summary>
        /// <param name="sender">Olayı tetikleyen nesne.</param>
        /// <param name="e">MouseMove olayı bilgisi.</param>
        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPanning)
            {
                Point scrollPos = scrollPanel.AutoScrollPosition;
                scrollPos = new Point(-scrollPos.X, -scrollPos.Y);

                int deltaX = e.X - mouseDownLocation.X;
                int deltaY = e.Y - mouseDownLocation.Y;

                scrollPanel.AutoScrollPosition = new Point(scrollPos.X - deltaX, scrollPos.Y - deltaY);
            }
        }

        /// <summary>
        /// Fare sol tuşu bırakıldığında çalışan olay: pan işlemini sonlandırır.
        /// </summary>
        /// <param name="sender">Olayı tetikleyen nesne.</param>
        /// <param name="e">MouseUp olayı bilgisi.</param>
        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isPanning = false;
                pictureBox.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Zoom oranını sıfırlar ve resmi orijinal boyutuna döndürür.
        /// Scroll pozisyonunu sol üst köşeye ayarlar.
        /// </summary>
        public void ResetZoom()
        {
            zoomFactor = minZoom; // Minimum zoom seviyesine ayarla (örneğin, 0.1f)

            if (pictureBox.Image != null)
            {
                // Görüntüyü panelin boyutlarına sığacak şekilde ölçeklendir
                int newWidth = (int)(pictureBox.Image.Width * zoomFactor);
                int newHeight = (int)(pictureBox.Image.Height * zoomFactor);

                // Görüntü boyutlarını panelin boyutlarına sığdır (isteğe bağlı)
                if (scrollPanel.ClientSize.Width > 0 && scrollPanel.ClientSize.Height > 0)
                {
                    float scaleX = (float)scrollPanel.ClientSize.Width / pictureBox.Image.Width;
                    float scaleY = (float)scrollPanel.ClientSize.Height / pictureBox.Image.Height;
                    float fitScale = Math.Min(scaleX, scaleY); // En küçük ölçek oranını kullan
                    zoomFactor = Math.Max(minZoom, fitScale); // minZoom'dan küçük olmasın
                    newWidth = (int)(pictureBox.Image.Width * zoomFactor);
                    newHeight = (int)(pictureBox.Image.Height * zoomFactor);
                }

                pictureBox.Size = new Size(newWidth, newHeight);
                pictureBox.Location = new Point(0, 0); // Sol üst köşeye hizala
            }
            else
            {
                // Görüntü yoksa, PictureBox boyutunu varsayılan yap
                pictureBox.Size = scrollPanel.ClientSize;
            }

            scrollPanel.AutoScrollPosition = new Point(0, 0);
            pictureBox.Invalidate();
            scrollPanel.Invalidate();
        }

        /// <summary>
        /// Geçerli zoom oranını döndürür.
        /// </summary>
        public float CurrentZoom => zoomFactor;
    }



}
