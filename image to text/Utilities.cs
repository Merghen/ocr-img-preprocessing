using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using Size = OpenCvSharp.Size;

namespace image_to_text
{

    public class Utilities
    {
        // Resim butonuna tıklanma durumunu saklamak için boolean değişkenler oluşturdum

        private bool img4_btn_clicked; 

        public Utilities()
        {
            // default olarak resim butona tıklanma durumunu false olarak ayarlıyorum

            img4_btn_clicked = false;
        }

        public string getImgBtnClicked()
        {
            // seçilen resmin buton bilgisini dönderiyorum
           
            if (img4_btn_clicked)
            {
                return "custom";
            }
            else
            {
                return "none";
            }
        }

        public void setImgBtnClick(string clickedButon)
        {

             if (clickedButon == "custom")
            {

                img4_btn_clicked = true;
            }
            else
            {

                img4_btn_clicked = false;
            }

        }

        
        public Mat setChosenImage(string path)
        {
            // Seçilen resim dosyasını yükler ve dönderir

            string img_path = path;
            Mat img = Cv2.ImRead(img_path);
            return img;
            

        }


        public Mat CropImage(Mat img, RectangleSelector selector)
        {
            // Orijinal görüntünün bir kopyası alınır
            Mat processedImg = img.Clone();
            Mat cropped = selector.Crop(img);

            

            if (cropped != null)
            {
                processedImg = cropped;
                
            }

            return processedImg;
        }

        public void AddApprovedImage(Mat image, List<Mat> approvedImages)
        {
            // Görselin geçerli olduğundan emin olun
            if (image != null && !image.Empty())
            {
                approvedImages.Add(image);
            }
        }

        public Mat CombineImagesHorizontally(List<Mat> approvedImages)
        {
            if (approvedImages == null || approvedImages.Count == 0)
                return null;

            int totalWidth = approvedImages.Sum(img => img.Cols);
            int maxHeight = approvedImages.Max(img => img.Rows);



            Mat mixedImg = new Mat(new Size(totalWidth, maxHeight), MatType.CV_8UC3, new Scalar(255, 255, 255));
            int currentX = 0;

            foreach (var img in approvedImages)
            {
                if (img.Channels() == 1)
                {
   
                    Cv2.CvtColor(img, img, ColorConversionCodes.GRAY2BGR);
                    
                }

                var roi = new OpenCvSharp.Rect(currentX, 0, img.Cols, img.Rows);
                img.CopyTo(new Mat(mixedImg, roi));
                currentX += img.Cols;
            }

            return mixedImg;
        }


        //public Mat getMixedImg(Mat img, RectangleSelector selector, List<Mat> ApprovedImages)
        //{
        //    // orijinal görüntünün bir kopyasını alıyorum
        //    Mat processedImg = img.Clone(); 

        //    // Kullanıcıdan gelen görüntüyü kırpma işlemi yaparak çıktı görseli oluşturuyor.
        //    Mat cropped = selector.Crop(img);


            
        //    //Eğer kullanıcı kırpma işlemi yaptıysa, processedImg değişkenini kırpılmış görüntü ile güncelliyorum  aksi halde orijinal görüntüyü kullanmaya devam ediyorum.
        //    if (cropped != null)
        //    {
        //        processedImg = cropped;
                
        //    }

            
        //    // Kullanıcının kestiği resmi listeye ekle
        //    ApprovedImages.Add(processedImg);


        //    // 2. Kırpılan resimleri yan yana birleştirmek için genişlik ve yükseklik hesaplamaları yapıyorum
        //    int totalWidth = ApprovedImages.Sum(img => img.Cols); // Toplam genişlik
        //    int maxHeight = ApprovedImages.Max(img => img.Rows); // Maksimum yükseklik

        //    // Birleştirilmiş görüntü için yeni bir Mat oluştur 
        //    Mat mixedImg = new Mat(new Size(totalWidth, maxHeight), MatType.CV_8UC3, new Scalar(255, 255, 255));
        //    int currentX = 0; // Yatay konum takibi

        //    // Onaylanan her  resmi birleştirerek mixedImg kopyaladım.
        //    foreach (var ApprovedImage in ApprovedImages)
        //    {

        //        // Her bir kırpılmış resmi birleştirilmiş görüntüye kopyala
        //        var roi = new OpenCvSharp.Rect(currentX, 0, ApprovedImage.Cols, ApprovedImage.Rows);
        //        ApprovedImage.CopyTo(new Mat(mixedImg, roi));
        //        currentX += ApprovedImage.Cols; // Bir sonraki resmin x koordinatını güncelle

        //    }
        //    return mixedImg;
        //}
    }
}
