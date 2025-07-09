using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tesseract;
namespace image_to_text
{
    /// <summary>
    /// Bu sınıf, görüntü üzerinde OCR (Optik Karakter Tanıma) işlemi yapar ve metin çıktısı üretir.
    /// </summary>
    public class OcrResult
    {

        /// <summary>
        /// Bu metod, ilgili görüntü üzerinde OCR işlemi gerçekleştirir ve metin çıktısı dönderir.
        /// </summary>
        /// <param name="img"> ocr işlemi yapılacak görsel </param>

        /// <returns>
        /// ocr işlemi sonucu elde edilen metin çıktısını döner.
        /// </returns>
        public String GetOcrResult(Mat img)
        {


            List<string> ocr_result = new List<string>(); // OCR sonuçlarını saklamak için

            Mat preprecces_img = img;

            // görüntüden yazılarımı çıkarıyorum
            using var ocr = new TesseractEngine(@"tessdata", "tur", EngineMode.Default);
            using var pix = PixConverter.ToPix(BitmapConverter.ToBitmap(preprecces_img));
            using var page = ocr.Process(pix);
            string text = page.GetText();
            // her satırı ele alarak bir dizi oluşturuyorum
            var lines = text.Split('\n');

            // dizimin içerisindeki her bir diziyi alıp sağdan soldan trim işlemi gerçekleştiriyorum ve dizime atıyorum
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    ocr_result.Add(line.Trim());
                }
            }

            // Her parçadan sonra boş satır ekle
            ocr_result.Add("");

            // dizinin string hale getiriyorum satırlar halinde.
            return string.Join("\n", ocr_result);
        }
    }
}
