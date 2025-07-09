using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace image_to_text.CustomParamPreprocces
{


    /// <summary>
    /// Bu sınıf, Blur (bulanıklaştırma) işlemi için gerekli parametreleri saklar.
    /// </summary>
    public class BlurringParams: ICustomParmPreprocces
    {
        public object FilterShape { get; set; }
        public string Type { get; set; } = "gaussan";
        public string Name { get; set; }
    }

    /// <summary>
    /// Bu sınıf, Threshold (eşikleme) işlemi için gerekli parametreleri saklar.
    /// </summary>
    public class ThresholdParams : ICustomParmPreprocces
    {
        public object MinThreshValue { get; set; }
        public object KernelShape { get; set; }
        public object CValue { get; set; }
        public string Type { get; set; } = "otsu threshold";
        public string Name { get; set; }
        public string Method { get; set; }
        public bool Isbitwise { get; set; }

    }

    /// <summary>
    /// Bu sınıf, morfolojik işlemi için gerekli parametreleri saklar.
    /// </summary>
    public class MorphologicalParams : ICustomParmPreprocces
    {
        public object FilterW { get; set; }
        public object FilterH { get; set; }
        public object Iterations { get; set; }
        public string Type { get; set; } = "erodion";
        public string Name { get; set; }
    }

    /// <summary>
    /// Bu sınıf, Contrast işlemi için gerekli parametreleri saklar.
    /// </summary>
    public class ContrastParams : ICustomParmPreprocces
    {
        public object ClipLimit { get; set; }
        public object GridSize { get; set; }
        public string Type { get; set; } 
        public string Name { get; set; }
    }

    /// <summary>
    /// Bu sınıf, kenar tespiti için gerekli parametreleri saklar.
    /// </summary>
    public class EdgeParams : ICustomParmPreprocces
    {
        public object KernelShape { get; set; }
        public object MinThrashold { get; set; }
        public object MaxThrashold { get; set; }
        public string Type { get; set; } = "canny";
        public string Name { get; set; }
    }


    /// <summary>
    /// Bu sınıf, Hough Transform için gerekli parametreleri saklar.
    /// </summary>
    public class HoughTransformParams : ICustomParmPreprocces
    {
        public object Thrashold { get; set; }
        public object MinLineLenght { get; set; }
        public object MaxLineLenght { get; set; }

        public object Dp { get; set; }
        public object MinDistance { get; set; }
        public object Param1 { get; set; }
        public object Param2 { get; set; }
        public object MinRadious { get; set; }
        public object MaxRadious { get; set; }

        public string Type { get; set; } = "lineP";
        public string Name { get; set; }
    }


    /// <summary>
    /// Bu sınıf, Blur, Threshold ve Morphological gibi görüntü işleme adımlarının parametrelerini saklar.
    /// Aynı zamanda dışarıdan gelen işleme göre doğru parametre nesnesini döndürür.
    /// </summary>
    public class CustomPreprocessParams
    {
        // Blur (bulanıklaştırma) işlemi için parametreler burada saklanır.
        public BlurringParams Bluring { get; set; } = new BlurringParams();

        // Threshold (eşikleme) işlemi için parametreler burada saklanır.
        public ThresholdParams Threshold { get; set; } = new ThresholdParams();

        // Morphological (erode, dilate vs.) işlemler için parametreler burada saklanır.
        public MorphologicalParams Morphological { get; set; } = new MorphologicalParams();

        // Contrast işlemi için parametreler burada saklanır.
        public ContrastParams Contrast { get; set; } = new ContrastParams();

        // Edge işlemi için parametreler burada saklanır.
        public EdgeParams Edge { get; set; } = new EdgeParams();

        // Hough Transform işlemi için parametreler burada saklanır.
        public HoughTransformParams HoughTransform { get; set; } = new HoughTransformParams();



        /// <summary>
        /// Bu metod, kullanıcıdan gelen ön işlem adım bilgisini alır ve ve kontrol ederek bu parametreye karşılık gelen nesneyi döndürür.
        /// </summary>
        /// <param name="methodName"> kişinin combobox ile seçtiği önişleme admını tutar.  </param>
        /// <returns>
        /// CustomParmPreprocces tipinde nesne dönderir. eğer seçilen işlem adı tanımlı değilse "null" döner.
        /// </returns>
        public ICustomParmPreprocces GetByName(string methodName)
        {
            // Önce işlem adı kontrol ediliyor, boş veya null ise null döner.
            if (string.IsNullOrEmpty(methodName))
            {
                return null;
            }

            // methodName küçük harfe çevrilerek karşılaştırma yapılır (büyük/küçük harf farkını önlemek için)
            methodName = methodName.ToLower();

            // Bu noktada switch-case yapısı kullanarak hangi işlem istenmiş ona bakıyoruz.
            switch (methodName)
            {
                case "blur":
                    // Eğer kullanıcı "blur" yazdıysa, Blur parametrelerini döndür.
                    return Bluring;

                case "threshold":
                    // Eğer kullanıcı "threshold" yazdıysa, Threshold parametrelerini döndür.
                    return Threshold;

                case "morphological operations":
                    // Eğer kullanıcı "morphological operations" yazdıysa, Morphology parametrelerini döndür.
                    return Morphological;

                case "contrast":
                    // Eğer kullanıcı "contrast" yazdıysa, Contrast parametrelerini döndür.
                    return Contrast;

                case "edge":
                    // Eğer kullanıcı "edge" yazdıysa, Edge parametrelerini döndür.
                    return Edge;

                case "hough transform":
                    // Eğer kullanıcı "hough transform" yazdıysa, Hough Transform parametrelerini döndür.
                    return HoughTransform;

                default:
                    // Eğer tanımsız bir işlem adı geldiyse, null döndür.
                    return null;
            }
        }
    }

}
