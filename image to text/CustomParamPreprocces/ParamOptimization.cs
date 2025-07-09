using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace image_to_text.CustomParamPreprocces
{
    /// <summary>
    /// Bu sınıf, seçilen önişleme adımlarına göre görüntü  üzerinde önişleme adımlarını uygular ve önişlenmiş görüntüyü döndürür.
    /// </summary>
    public class ParamOptimization
    {

        /// <summary>
        /// Bu metod, ön işlenmiş görüntüyü döndürür.
        /// </summary>
        /// <param name="approvedImage"> dışardan seçilen görüntüyü tutar. </param>
        /// <param name="preproccesMethod"> kişinin combobox ile seçtiği önişleme admını tutar.  </param>
        /// <param name="customParams"> CustomParmPreprocces sınıfdan oluşturulan ön işleme parametrelerini tutar.  </param>
        /// <returns>
        /// Bitmap tipinde önişlenmiş görüntü döner.
        /// </returns>
        public Bitmap StartPreviewFrame(Mat approvedImage, string preproccesMethod, CustomPreprocessParams customParams)
        {
            Mat workingImage = approvedImage.Clone();
            ApplyCurrentStep(workingImage, preproccesMethod, customParams);

            // Derinliği kontrol et ve gerekirse dönüştür. Bazı ön işleme adımları derinliği değiştirebilir. Ama ekranda gösterilecek görüntü için 8U formatı tercih ediliyor.
            if (workingImage.Depth() != MatType.CV_8U)
            {
                Cv2.Normalize(workingImage, workingImage, 0, 255, NormTypes.MinMax);
                Cv2.ConvertScaleAbs(workingImage, workingImage);  // mutlak değeri al + 8U formatına çevir
            }
            else
            {
                workingImage = workingImage.Clone();
            }

            return BitmapConverter.ToBitmap(workingImage);
        }

        /// <summary>
        /// Bu metod, dışardan seçilen metod yöntemini baz alarak ilgili ön işleme nesenesini alır ve görüntü üzerinde uygular.
        /// </summary>
        /// <param name="image"> dışardan seçilen görüntüyü tutar. </param>
        /// <param name="preproccesMethod"> kişinin combobox ile seçtiği önişleme admını tutar.  </param>
        /// <param name="customParams"> CustomParmPreprocces sınıfdan oluşturulan ön işleme parametrelerini tutar.  </param>
        private void ApplyCurrentStep(Mat image, String preproccesMethod, CustomPreprocessParams customParams)
        {

            var currentStep = customParams.GetByName(preproccesMethod);
            if (currentStep != null)
            {
                ApplyStep(currentStep, ref image);
            }
        }

        /// <summary>
        /// Bu metod, seçilen güncel ön işleme göre görüntü üzerinde gerekli ön işleme adımlarını  uygular.
        /// </summary>
        /// <param name="step"> CustomParmPreprocces tipinde kişinin seçitği önişleme adımın bilgisini tutar. </param>
        /// <param name="image"> ilgili görseli tutar.  </param>
        public void ApplyStep(ICustomParmPreprocces step, ref Mat image) // değişikliğin image üzerinde kalıcı olması için ref kullanıldı.
        {
            if (step == null || image == null || image.Empty())
                return;
           
            switch (step)
            {

                // ThresholdParams th olarak erişiyorum.
                case ThresholdParams th:
                    if (image.Channels() == 3)
                    {
                        Cv2.CvtColor(image, image, ColorConversionCodes.BGR2GRAY);
                    }

                    if (th.Type == "threshold")
                    {
                        
                        Cv2.Threshold(image, image, Convert.ToDouble(th.MinThreshValue), 255, ThresholdTypes.Binary);
                        
                    }
                    else if (th.Type == "adaptive threshold")
                    {
                        AdaptiveThresholdTypes method;

                        if (th.Method == "mean")
                            method = AdaptiveThresholdTypes.MeanC;
                        else if (th.Method == "gaussian")
                            method = AdaptiveThresholdTypes.GaussianC;
                        else
                            method = AdaptiveThresholdTypes.MeanC; // Varsayılan değer

                        Cv2.AdaptiveThreshold(
                            image,
                            image,
                            255,
                            method,
                            ThresholdTypes.Binary,
                            Convert.ToInt32(th.KernelShape),
                            Convert.ToDouble(th.CValue)
                        );
                    }
                    else if (th.Type == "otsu threshold")
                    {
                        
                        Cv2.Threshold(image, image, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                    }

                    if (th.Isbitwise)
                    {

                        Cv2.BitwiseNot(image, image);
                    }



                    break;

                case BlurringParams blur:
                    int kernelSize = Convert.ToInt32(blur.FilterShape);

                    if (blur.Type == "gaussen")
                    {
                        Cv2.GaussianBlur(image, image, new Size(kernelSize, kernelSize), 0);
                    }
                    else if (blur.Type == "median")
                    {
                        Cv2.MedianBlur(image, image, kernelSize);
                    }
                    else if (blur.Type == "blur")
                    {
                        Cv2.Blur(image, image, new Size(kernelSize, kernelSize));
                    }
                    break;

                case MorphologicalParams morph:
                    Mat kernel = Cv2.GetStructuringElement(
                        MorphShapes.Rect,
                        new Size(Convert.ToInt32(morph.FilterW), Convert.ToInt32(morph.FilterH))
                    );

                    string type = morph.Type?.ToLower();

                    if (type == "erodion" )
                    {
                        Cv2.Erode(image, image, kernel, iterations: Convert.ToInt32(morph.Iterations));
                    }
                    else if (type == "dilation" )
                    {
                        Cv2.Dilate(image, image, kernel, iterations: Convert.ToInt32(morph.Iterations));
                    }
                    else if (type == "opening")
                    {
                        Cv2.MorphologyEx(image, image, MorphTypes.Open, kernel, iterations: Convert.ToInt32(morph.Iterations));
                    }
                    else if (type == "closing")
                    {
                        Cv2.MorphologyEx(image, image, MorphTypes.Close, kernel, iterations: Convert.ToInt32(morph.Iterations));
                    }
                    break;

                case ContrastParams contrast:

                    if (image.Channels() == 3)
                    {
                        Cv2.CvtColor(image, image, ColorConversionCodes.BGR2GRAY);
                    }

                    int clipLimit = Convert.ToInt32(contrast.ClipLimit);
                    int gridSize = Convert.ToInt32(contrast.GridSize);

                    Cv2.CreateCLAHE(clipLimit, new Size(gridSize, gridSize)).Apply(image, image);

                    break;

                case EdgeParams edge:

                    if (image.Channels() == 3)
                    {
                        Cv2.CvtColor(image, image, ColorConversionCodes.BGR2GRAY);

                        
                    }

                    if (edge.Type == "canny")
                    {
                        Cv2.Canny(image, image, Convert.ToDouble(edge.MinThrashold), Convert.ToDouble(edge.MaxThrashold));
                        
                    }

                    else if (edge.Type == "sobel")
                    {
                        Cv2.Sobel(image, image, MatType.CV_8U, 1, 1, ksize: Convert.ToInt32(edge.KernelShape));
                    }

                    else if (edge.Type == "sobelX")
                    {
                        Cv2.Sobel(image, image, MatType.CV_8U, 1, 0, ksize: Convert.ToInt32(edge.KernelShape));
                    }

                    else if (edge.Type == "sobelY")
                    {
                        Cv2.Sobel(image, image, MatType.CV_8U, 0, 1, ksize: Convert.ToInt32(edge.KernelShape));
                    }

                    else if (edge.Type == "laplacian")
                    {
                        Cv2.Laplacian(image, image, MatType.CV_8U, ksize: Convert.ToInt32(edge.KernelShape));
                    }

                    break;


                case HoughTransformParams hough:

                    if (image.Channels() == 3)
                    {
                        Cv2.CvtColor(image, image, ColorConversionCodes.BGR2GRAY);
                    }

                    if (hough.Type == "houghP")
                    {
                        LineSegmentPoint[] lines = Cv2.HoughLinesP(
                            image,
                            rho: 1,
                            theta: Math.PI / 180,
                            threshold: Convert.ToInt32(hough.Thrashold),
                            minLineLength: Convert.ToInt32(hough.MinLineLenght),
                            maxLineGap: Convert.ToInt32(hough.MaxLineLenght)
                         );

                        // Çizgileri çiz
                        foreach (var line in lines)
                        {
                            Cv2.Line(image, line.P1, line.P2, new Scalar(255, 0, 0), 2);
                        }

                    }

                    else if (hough.Type == "houghC")
                    {
                        CircleSegment[] circles = Cv2.HoughCircles(
                            image,
                            HoughModes.Gradient,
                            dp: Convert.ToInt32(hough.Dp),
                            minDist: Convert.ToInt32(hough.MinDistance),
                            param1: Convert.ToInt32(hough.Param1),
                            param2: Convert.ToInt32(hough.Param2),
                            minRadius: Convert.ToInt32(hough.MinRadious),
                            maxRadius: Convert.ToInt32(hough.MaxRadious)

                         );

                        foreach (var circle in circles)
                        {
                            Point center = new Point(circle.Center.X, circle.Center.Y);
                            int radius = (int)circle.Radius;

                            // Dairenin kendisini çiz
                            Cv2.Circle(image, center, radius, new Scalar(255, 0, 0), 2);
                        }
                    }

                    break;

                default:
                    
                    break;
            }
        }


    }
}