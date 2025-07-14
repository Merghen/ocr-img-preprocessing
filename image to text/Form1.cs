using image_to_text.CustomParamPreprocces;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Windows.Forms;
using Tesseract;
using static OpenCvSharp.SimpleBlobDetector;
using Point = System.Drawing.Point;
using Size = OpenCvSharp.Size;



namespace image_to_text
{

    public partial class Form1 : Form
    {
        // global değişkenleri tanımlıyorum
        Utilities util = new Utilities();
        Mat img = new Mat();
        List<Mat> ApprovedImages = new List<Mat>(); // Kullanıcının onayladığı OCR için hazır resimleri saklamak için liste
        List<Mat> UnReadyImages = new List<Mat>(); // Kullanıcının seçtiği görselleri ekranda gösteren ancak ocr için hazır olmayan resimleri saklamak için liste
        String OrcResult; // OCR sonuçlarını saklamak için kullanılan string değişken
        string preproccesMethod; //seçilen önişleme adımına ait bilgiyi tutan değişken

        ParamOptimization paramOptimization = new ParamOptimization(); // ön işleme adımlarını yaptığım sınıfın örneği
        int currentImageIndex = 0; // önişleme işleminin yapıldığı güncel görüntü indeksini tutan değişken
        CustomPreprocessParams customParams = new CustomPreprocessParams(); // önişlemelere ait parametreleri tutan sınıfın örneği
        ImageZoomController zoomController;
        List<ICustomParmPreprocces> appliedSteps = new List<ICustomParmPreprocces>(); // Kullanıcının onayladığı ön işleme adımlarını saklamak için list

        // RectangleSelector sınıfı, kullanıcıların resim üzerinde dikdörtgen seçim yapmasına olanak tanır.
        private RectangleSelector selector;
        public Form1()
        {
            InitializeComponent();

            // görüntü kutumun içindeki eventleri dinleyerek güncelliyorum
            selector = new RectangleSelector(upload_picBox);

            // Başlangıç için default ön işleme değerlerini atıyoruz
            customParams = new CustomPreprocessParams
            {
                Bluring = new BlurringParams
                {
                    FilterShape = blr_filter_shape_nud.Value,
                    Name = null
                },
                Threshold = new ThresholdParams
                {
                    MinThreshValue = null,
                    KernelShape = null,
                    CValue = null,
                    Name = null
                },
                Morphological = new MorphologicalParams
                {
                    FilterW = mrflj_fltr_wdth_nud.Value,
                    FilterH = mrflj_fltr_hgt_nud.Value,
                    Iterations = mrflj_iteration_nud.Value,
                    Name = null
                },
                Contrast = new ContrastParams
                {
                    ClipLimit = clip_limit_nud.Value,
                    GridSize = tile_grid_size_nud.Value,
                    Name = null
                },

                Edge = new EdgeParams
                {

                    KernelShape = edg_ksize_nud.Value, // Varsayılan değer

                    Name = null
                },

                HoughTransform = new HoughTransformParams
                {
                    MinLineLenght = stdHoughL_minLine_nud.Value,
                    MaxLineLenght = stdHoughL_maxLine_nud.Value,
                    Thrashold = stdHoughL_thrs_nud.Value,
                    Dp = stdHoughC_dp_nud.Value,
                    MinDistance = stdHoughC_minDistance_nud.Value,
                    Param1 = stdHoughC_param1_nud.Value,
                    Param2 = stdHoughC_param2_nud.Value,
                    MinRadious = stdHoughC_minRadious_nud.Value,
                    MaxRadious = stdHoughC_maxRadious_nud.Value,
                    Name = null,
                    Type = "houghP"

                }

            };


            // Gerekli ayarlar
            zoomController = new ImageZoomController(currentImg_picbox, ApprovedImgPanel);
        }

        /// <summary>
        /// Verilen resim yolundan resmi okuyup yükler, ilgili görsel bilgilerini günceller
        /// ve PictureBox'ta görüntüler.
        /// </summary>
        /// <param name="img_path">Yüklenecek resmin dosya yolu.</param>
        /// <param name="img_info">Resim ile ilişkili bilgi (örneğin buton etiketi).</param>
        private void read_and_set_img(string img_path, string img_info)
        {
            // Seçilen resim dosyasının yüklüyoruz
            img = util.setChosenImage(img_path);

            // Seçilen resim bilgisini güncelleme
            util.setImgBtnClick(img_info);

            // görüntüyü tam sığdımak için otomatik boyutlandırma ve PictureBox'a atıyorum
            upload_picBox.SizeMode = PictureBoxSizeMode.Zoom; // Görüntüyü tam sığdırmak için otomatik boyutlandırma
            upload_picBox.Image = BitmapConverter.ToBitmap(img);
        }


        /// <summary>
        /// Uygulanan ve geçici olan ön işleme adımlarını ListBox'ta listeler.
        /// </summary>
        private void UpdateListBox()
        {
            listBox1.Items.Clear(); // Önce liste kutusunu temizle

            // Eğer kullanıcı tarafından onaylanan işlemler varsa, bunları listeleriz
            if (appliedSteps.Any())
            {
                listBox1.Items.Add("Onaylanmış Yöntemler:");

                foreach (var step in appliedSteps)
                {
                    // İşlem adı ve türü ilk olarak yazdırılır
                    listBox1.Items.Add($"Name: {step.Name}");
                    listBox1.Items.Add($"Type: {step.Type}");

                    // Diğer tüm özellikler (parametreler) yazdırılır
                    // "Name" ve "Type" dışındakiler
                    if (step is BlurringParams blur)
                    {
                        listBox1.Items.Add($"FilterShape: {blur.FilterShape}");
                    }
                    else if (step is ThresholdParams thresh)
                    {
                        listBox1.Items.Add($"MinThreshValue: {thresh.MinThreshValue}");
                        listBox1.Items.Add($"KernelShape: {thresh.KernelShape}");
                        listBox1.Items.Add($"CValue: {thresh.CValue}");
                    }
                    else if (step is MorphologicalParams morph)
                    {
                        listBox1.Items.Add($"FilterW: {morph.FilterW}");
                        listBox1.Items.Add($"FilterH: {morph.FilterH}");
                        listBox1.Items.Add($"Iterations: {morph.Iterations}");
                    }

                    listBox1.Items.Add(""); // Her adım sonunda boş satır bırak
                }
            }

            // Eğer kullanıcı bir işlem seçmiş ama onaylamamışsa, geçici hali de gösterilir
            if (!string.IsNullOrEmpty(preproccesMethod))
            {
                var currentStep = customParams.GetByName(preproccesMethod);

                listBox1.Items.Add("Önizleme (Onaylanmadı):");

                if (currentStep != null)
                {
                    listBox1.Items.Add($"Name: {currentStep.Name}");
                    listBox1.Items.Add($"Type: {currentStep.Type}");

                    // Geçici adımın parametrelerini yazdır
                    if (currentStep is BlurringParams blur)
                    {
                        listBox1.Items.Add($"FilterShape: {blur.FilterShape}");
                    }
                    else if (currentStep is ThresholdParams thresh)
                    {
                        listBox1.Items.Add($"MinThreshValue: {thresh.MinThreshValue}");
                        listBox1.Items.Add($"KernelShape: {thresh.KernelShape}");
                        listBox1.Items.Add($"CValue: {thresh.CValue}");
                    }
                    else if (currentStep is MorphologicalParams morph)
                    {
                        listBox1.Items.Add($"FilterW: {morph.FilterW}");
                        listBox1.Items.Add($"FilterH: {morph.FilterH}");
                        listBox1.Items.Add($"Iterations: {morph.Iterations}");
                    }
                }
            }
        }

        /// <summary>
        /// Tüm ön işleme parametrelerini varsayılan değerlere sıfırlar.
        /// </summary>
        private void SetPrepreccesStepsDefault()
        {
            // ayarları varsayalına dönüştüyorum
            mrflj_iteration_nud.Value = 1;
            mrflj_fltr_hgt_nud.Value = 1;
            mrflj_fltr_wdth_nud.Value = 1;
            thrs_adptv_cValue_nud.Value = 1;
            thrs_adptv_krnlSize_nud.Value = 3;
            thrs_min_value_nud.Value = 1;
            thrshold_rdbtn.Enabled = true;
            thrshold_rdbtn.Checked = true;
            blr_filter_shape_nud.Value = 1;
            bitwise_thrshold_chckBx.Checked = false;
            clip_limit_nud.Value = 1;
            tile_grid_size_nud.Value = 1;

            edg_ksize_nud.Value = 1;
            edg_cannyMaxThrs_nud.Value = 1;
            edg_cannyMinThrs_nud.Value = 1;

            stdHoughL_minLine_nud.Value = 1;
            stdHoughL_maxLine_nud.Value = 1;
            stdHoughL_thrs_nud.Value = 1;
            stdHoughC_dp_nud.Value = 1;
            stdHoughC_minDistance_nud.Value = 1;
            stdHoughC_param1_nud.Value = 1;
            stdHoughC_param2_nud.Value = 1;
            stdHoughC_minRadious_nud.Value = 1;
            stdHoughC_maxRadious_nud.Value = 1;

        }

        /// <summary>
        /// Onaylanmış görüntü üzerinden geçici ön işleme adımını uygular ve sonucu görüntüler.
        /// </summary>
        private async void UpdatePreviewImage()
        {
            if (ApprovedImages.Count > 0)
            {
                // sonsuz bir döngü oluşturduğum için ekranın kilitlenmemes için async fonksiyon kullandım. pictureBox'ı anlık güncelliyorum.
                Bitmap previewImage = await Task.Run(() =>
                {
                    return paramOptimization.StartPreviewFrame(ApprovedImages[currentImageIndex], preproccesMethod, customParams);
                });

                currentImg_picbox.Image?.Dispose();
                currentImg_picbox.SizeMode = PictureBoxSizeMode.Zoom;
                currentImg_picbox.Image = previewImage;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        private void setGroupBoxesDefault()
        {
            morfolojk_grpbx.Visible = false;
            threshold_grpbx.Visible = false;
            blur_grpbx.Visible = false;
            contrast_grpbx.Visible = false;
            edge_grpbx.Visible = false;
            stdHough_grpx.Visible = false;
            onisleme_kydt_btn.Visible = false;
        }



        private void custom_img_btn_Click(object sender, EventArgs e)
        {
            // Kullanıcıdan özel bir resim seçmesini istiyoruz
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.tiff",
                Title = "Select an Image File"
            };

            // eğer kullanıcı bir resim seçerse
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // görüntünün yolunu belirliyoruz.
                string imgPath = openFileDialog.FileName;
                read_and_set_img(imgPath, "custom");

            }
        }
        private void onayla_btn_Click(object sender, EventArgs e)
        {
            // seçilen resim butonuna ait bilgiyi al
            string secilenBtn = util.getImgBtnClicked();

            // Eğer görüntü seçiliyse
            if (secilenBtn != "none")
            {
                // kişi resim üzerinde kırpma işlemi yapmışsa bu görüntüyü kırpıp dönderiyorum kırpılmamışsa aynı görüntü dönderir.
                Mat processedImg = util.CropImage(img, selector);
                Mat rawCopy = processedImg.Clone(); // görselin kopyasını alıyorum, böylece orijinal görüntüye dokunmadan işlem yapabilirim

                // Onaylanan resimlerin listesini güncelliyorum
                util.AddApprovedImage(processedImg, ApprovedImages);
                // Onaylanan resimleri birleştiriyorum ve 1 görsel elde ediyorum.
                Mat mixedImg = util.CombineImagesHorizontally(ApprovedImages);

                // görüntüyü tam sığdımak için otomatik boyutlandırma ve PictureBox'a atıyorum
                approvedImg_picbox.SizeMode = PictureBoxSizeMode.Zoom;
                approvedImg_picbox.Image = BitmapConverter.ToBitmap(mixedImg);



                // ocr için hazır olmayan görselleri ham görseli UnReadyImages listesine ekliyorum
                UnReadyImages.Add(rawCopy);
                // ocr için hazır olmayan görselleri birleştiriyorum ve 1 görsel elde ediyorum.
                Mat mixedImg2 = util.CombineImagesHorizontally(UnReadyImages);

                // görüntüyü tam sığdımak için otomatik boyutlandırma ve PictureBox'a atıyorum
                unreadyImg_picbox.SizeMode = PictureBoxSizeMode.Zoom;
                unreadyImg_picbox.Image = BitmapConverter.ToBitmap(mixedImg2);




                // ilk başta seçim yapıldığında prewierImageBoxa atama yapıyorum
                currentImg_picbox.SizeMode = PictureBoxSizeMode.Zoom;
                currentImg_picbox.Image = BitmapConverter.ToBitmap(ApprovedImages[currentImageIndex]);



                // görüntü eklendiğinde upload_picBox otomatik sıfırlansın 
                upload_picBox.Image?.Dispose();
                upload_picBox.Image = null;
                util.setImgBtnClick("none");

            }
            // Eğer kullanıcı bir resim seçmemişse, kullanıcıya bir mesaj gösteriyoruz
            else
            {
                MessageBox.Show("Please choose an image.");
            }
        }
        private void img_to_text_btn_Click(object sender, EventArgs e)
        {
            // her OCR işlemi için bu butona basıldığında, sonuçları temizleyerek yeni görüntülerden elde edilen metinleri temiz şekilde başlatıyoruz
            result_txt_box.Text = "";

            // Seçilen resme ait bilgiyi alıyoruz
            string secilenBtn = util.getImgBtnClicked();

            // Eğer kullanıcı herhangi bir resmi onaylamadıysa kullanıcıya bir mesaj gösteriyoruz
            if (ApprovedImages.Count == 0)
            {
                MessageBox.Show("Please select an image and confirm.");
                return; // Eğer kullanıcı bir resim seçmemişse, işlemi sonlandırıyoruz
            }

            // Eğer kullanıcı resim onayladıysa, onaylanan resimler üzerinde işlemler yaparak OCR  çıktısını alıyoruz
            else
            {
                String text;

                // Onaylanan her bir görüntü için
                for (int i = 0; i < ApprovedImages.Count; i++)
                {
                    // Onaylanan her resim ve ona ait kaynak bilgilerini alıyoruz
                    var currentImg = ApprovedImages[i];


                    OcrResult ocrResult = new OcrResult();
                    //OCR sonuçlarını metin olarak alıyoruz.

                    text = ocrResult.GetOcrResult(currentImg);

                    // eğer birden fazla görüntüden metin alındıysa, metinleri birleştiriyoruz

                    OrcResult += "------------------------Next Image-----------------------\n" + text;
                }
            }

            result_txt_box.Text = OrcResult; // İşlenmiş metni TextBox'a yazdır
            OrcResult = ""; // OCR sonuçlarını temizle. Yeni bir işlem yaptığında var olan metinlerin üzerine yazılmasını engellemek için.
        }
        private void onIsleme_combobx_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (onIsleme_combobx.SelectedIndex == -1)
            {
                return;
            }

            if (ApprovedImages.Count > 0)
            {
                // seçilen ön işleme adımını al
                preproccesMethod = onIsleme_combobx.SelectedItem?.ToString();

                // Tüm grup kutularını gizle (önce sıfırla)
                setGroupBoxesDefault();

                // Onayla butonunu göster ve yerini ayarla (default)
                onisleme_kydt_btn.Visible = true;

                // Önişleme adımına göre ayarlamalar
                switch (preproccesMethod)
                {
                    case "Morphological Operations":
                        morfolojk_grpbx.Visible = true;
                        onisleme_kydt_btn.Location = new Point(341, 265);
                        customParams.Morphological.Name = "morphological";
                        break;

                    case "Threshold":
                        threshold_grpbx.Visible = true;
                        onisleme_kydt_btn.Location = new Point(357, 415);
                        customParams.Threshold.Name = "threshold";
                        break;

                    case "Blur":
                        blur_grpbx.Visible = true;
                        onisleme_kydt_btn.Location = new Point(345, 217);
                        customParams.Bluring.Name = "bluring";
                        break;

                    case "Contrast":
                        contrast_grpbx.Visible = true;
                        onisleme_kydt_btn.Location = new Point(359, 231);
                        customParams.Contrast.Name = "contrast";
                        break;
                    case "Edge":
                        edge_grpbx.Visible = true;
                        onisleme_kydt_btn.Location = new Point(363, 392);
                        customParams.Edge.Name = "edge";
                        break;
                    case "Hough Transform":
                        stdHough_grpx.Visible = true;
                        onisleme_kydt_btn.Location = new Point(360, 421);
                        customParams.HoughTransform.Name = "hough transform";
                        break;
                }

                // Önizleme ve liste güncelle
                UpdatePreviewImage();
                UpdateListBox();
            }
            else
            {
                MessageBox.Show("Please select an image and confirm");
            }

        }


        private void onisleme_kydt_btn_Click_1(object sender, EventArgs e)
        {
            // kaydet butonuna basıldığında ekrandaki bilglileri alıp atama işlemi yapar ve appliedSteps dizimin içerisinde onaylanan her bir ön işleme adımını tutar.
            if (ApprovedImages.Count > 0)
            {
                
                if (preproccesMethod == "Blur")
                {
                    appliedSteps.Add(new BlurringParams
                    {
                        FilterShape = customParams.Bluring.FilterShape,
                        Type = customParams.Bluring.Type,
                        Name = customParams.Bluring.Name
                    });
                }
                else if (preproccesMethod == "Threshold")
                {
                    appliedSteps.Add(new ThresholdParams
                    {
                        MinThreshValue = customParams.Threshold.MinThreshValue,
                        KernelShape = customParams.Threshold.KernelShape,
                        CValue = customParams.Threshold.CValue,
                        Type = customParams.Threshold.Type,
                        Name = customParams.Threshold.Name
                    });
                }

                else if (preproccesMethod == "Morphological Operations")
                {
                    appliedSteps.Add(new MorphologicalParams
                    {
                        FilterW = customParams.Morphological.FilterW,
                        FilterH = customParams.Morphological.FilterH,
                        Iterations = customParams.Morphological.Iterations,
                        Type = customParams.Morphological.Type,
                        Name = customParams.Morphological.Name
                    });
                }

                else if (preproccesMethod == "Contrast")
                {
                    appliedSteps.Add(new ContrastParams
                    {
                        ClipLimit = customParams.Contrast.ClipLimit,
                        GridSize = customParams.Contrast.GridSize,
                        Type = customParams.Contrast.Type,
                        Name = customParams.Contrast.Name
                    });
                }

                else if (preproccesMethod == "Edge")
                {
                    appliedSteps.Add(new EdgeParams
                    {
                        MinThrashold = customParams.Edge.MinThrashold,
                        MaxThrashold = customParams.Edge.MaxThrashold,
                        KernelShape = customParams.Edge.KernelShape,
                        Type = customParams.Contrast.Type,
                        Name = customParams.Contrast.Name
                    });
                }

                else if (preproccesMethod == "Hough Transform")
                {
                    appliedSteps.Add(new HoughTransformParams
                    {
                        Thrashold = customParams.HoughTransform.Thrashold,
                        MinLineLenght = customParams.HoughTransform.MinLineLenght,
                        MaxLineLenght = customParams.HoughTransform.MaxLineLenght,
                        Dp = customParams.HoughTransform.Dp,
                        MinDistance = customParams.HoughTransform.MinDistance,
                        Param1 = customParams.HoughTransform.Param1,
                        Param2 = customParams.HoughTransform.Param2,
                        MinRadious = customParams.HoughTransform.MinRadious,
                        MaxRadious = customParams.HoughTransform.MaxRadious,
                        Type = customParams.HoughTransform.Type,
                        Name = customParams.HoughTransform.Name
                    });
                }


                else
                {
                    MessageBox.Show("Please choose a valid preprocces method.");
                    return;
                }



                // Seçilen önişleme adımını alıyorum
                var step = customParams.GetByName(preproccesMethod);
                // Eğer listede görüntü varsa, o görüntüyü alıp onun üzerine işlem yapıyorum
                if (ApprovedImages.Count > 0)
                {

                    // Mevcut görüntünün referansını al
                    var currentImage = ApprovedImages[currentImageIndex];

                    // İşlemi uygula (referansla)
                    paramOptimization.ApplyStep(step, ref currentImage);

                    // Listeyi güncelle (gerekirse)
                    ApprovedImages[currentImageIndex] = currentImage;
                }



                // Preview başlat
                Bitmap previewImage = paramOptimization.StartPreviewFrame(ApprovedImages[currentImageIndex], preproccesMethod, customParams);

                // UI thread'inde güncelle
                currentImg_picbox.Image?.Dispose();
                currentImg_picbox.SizeMode = PictureBoxSizeMode.Zoom;
                currentImg_picbox.Image = previewImage;



                // onaylanan picBox'u güncelliyorum
                Mat mixedImg = util.CombineImagesHorizontally(ApprovedImages);

                approvedImg_picbox.Image?.Dispose();
                approvedImg_picbox.SizeMode = PictureBoxSizeMode.Zoom;
                approvedImg_picbox.Image = BitmapConverter.ToBitmap(mixedImg);

                UpdateListBox(); // Listeyi güncelle
                // her  onayla işleminden sonra ön işleme parametrelerini varsayılana dönderiyiorum
                SetPrepreccesStepsDefault();
            }
            else
            {
                MessageBox.Show("Please Select Image and Confirm");
            }

        }


        private void cln_img_Click(object sender, EventArgs e)
        {
            ApprovedImages.Clear(); // Kırpılmış resimleri temizle
            UnReadyImages.Clear(); // Onaylanmamış resimleri temizle
            upload_picBox.Image = null; // yüklenen resmi temizle
            unreadyImg_picbox.Image = null; // onaylanan resmi temizle
            result_txt_box.Text = ""; // TextBox'taki metni temizle
            OrcResult = ""; // OCR sonuçlarını temizle
            util.setImgBtnClick("none"); // Seçilen resim butonunu ait bilgiyi güncelliyoruz
            currentImg_picbox.Image = null; // Preview görüntüsünü temizle
            approvedImg_picbox.Image = null;
            onIsleme_combobx.SelectedIndex = -1; // Ön işleme combobox'ını sıfırla
            currentImageIndex = 0;
            setGroupBoxesDefault(); // Grup kutularını varsayılana döndür
            SetPrepreccesStepsDefault();

        }



        private void mrflj_fltr_wdth_nud_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre değiştiğinde otomatik görüntüye uygulatıp ekranda gösteriyorum
            customParams.Morphological.FilterW = mrflj_fltr_wdth_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();

        }


        private async void mrflj_iteration_nud_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre değiştiğinde otomatik görüntüye uygulatıp ekranda gösteriyorum
            customParams.Morphological.Iterations = mrflj_iteration_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();

        }

        private async void mrflj_fltr_hgt_nud_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre değiştiğinde otomatik görüntüye uygulatıp ekranda gösteriyorum
            customParams.Morphological.FilterH = mrflj_fltr_hgt_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();

        }

        private async void mrflj_erodion_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (mrflj_erodion_rdbtn.Checked)
            {
                // ilgili parametre değiştiğinde otomatik görüntüye uygulatıp ekranda gösteriyorum
                customParams.Morphological.Type = "erodion";
                UpdateListBox();
                UpdatePreviewImage();

            }
        }

        private async void mrflj_dilation_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (mrflj_dilation_rdbtn.Checked)
            {
                // ilgili parametre değiştiğinde otomatik görüntüye uygulatıp ekranda gösteriyorum
                customParams.Morphological.Type = "dilation";
                UpdateListBox();
                UpdatePreviewImage();

            }
        }

        private async void mrflj_opening_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (mrflj_opening_rdbtn.Checked)
            {
                // ilgili parametre değiştiğinde otomatik görüntüye uygulatıp ekranda gösteriyorum
                customParams.Morphological.Type = "opening";
                UpdateListBox();
                UpdatePreviewImage();

            }
        }

        private async void mrflj_closing_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (mrflj_closing_rdbtn.Checked)
            {
                // ilgili parametre değiştiğinde otomatik görüntüye uygulatıp ekranda gösteriyorum
                customParams.Morphological.Type = "closing";
                UpdateListBox();
                UpdatePreviewImage();

            }
        }

        private void thrs_adptv_krnlSize_nud_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre değiştiğinde otomatik görüntüye uygulatıp ekranda gösteriyorum
            customParams.Threshold.KernelShape = thrs_adptv_krnlSize_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();

        }

        private void thrs_adptv_cValue_nud_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre değiştiğinde otomatik görüntüye uygulatıp ekranda gösteriyorum
            customParams.Threshold.CValue = thrs_adptv_cValue_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre değiştiğinde otomatik görüntüye uygulatıp ekranda gösteriyorum
            customParams.Threshold.MinThreshValue = thrs_min_value_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();
        }

        private void thrshold_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (thrshold_rdbtn.Checked)
            {
                // ilgili parametre değiştiğinde otomatik görüntüye uygulatıp ekranda gösteriyorum ve alaksaız olan diğer parametrelerin görünürlüğünü güncelliyorum
                thrs_min_value_nud.Enabled = true;
                thrs_adptv_cValue_nud.Enabled = false;
                thrs_adptv_krnlSize_nud.Enabled = false;
                thrs_metod_gaussian_rdbx.Enabled = false; // adaptive threshold seçildiğinde diğer seçenekleri devre dışı bırakıyorum
                thrs_metod_mean_rdbx.Enabled = false;

                customParams.Threshold.Type = "threshold";
                customParams.Threshold.MinThreshValue = thrs_min_value_nud.Value;
                customParams.Threshold.CValue = null;
                customParams.Threshold.KernelShape = null;

                UpdateListBox();
                UpdatePreviewImage();
            }
        }

        private void adaptive_thrshold_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (adaptive_thrshold_rdbtn.Checked)
            {
                // ilgili parametre değiştiğinde otomatik görüntüye uygulatıp ekranda gösteriyorum ve alaksaız olan diğer parametrelerin görünürlüğünü güncelliyorum
                thrs_min_value_nud.Enabled = false;
                thrs_adptv_cValue_nud.Enabled = true;
                thrs_adptv_krnlSize_nud.Enabled = true;
                thrs_metod_gaussian_rdbx.Enabled = true; // adaptive threshold seçildiğinde diğer seçenekleri devre dışı bırakıyorum
                thrs_metod_mean_rdbx.Enabled = true;


                customParams.Threshold.Type = "adaptive threshold";
                customParams.Threshold.MinThreshValue = null;
                customParams.Threshold.CValue = thrs_adptv_cValue_nud.Value;
                customParams.Threshold.KernelShape = thrs_adptv_krnlSize_nud.Value;


                UpdateListBox();
                UpdatePreviewImage();
            }
        }

        private void bitwise_thrshold_chckBx_CheckedChanged(object sender, EventArgs e)
        {
            customParams.Threshold.Isbitwise = bitwise_thrshold_chckBx.Checked; // Bitwise threshold seçildiğinde bu metodu kullanacak şekilde parametreyi ayarlıyorum
            UpdatePreviewImage();
        }

        private void thrs_metod_gaussian_rdbx_CheckedChanged(object sender, EventArgs e)
        {
            customParams.Threshold.Method = "gaussian"; // Gaussian method seçildiğinde bu metodu kullanacak şekilde parametreyi ayarlıyorum
            UpdatePreviewImage();
        }

        private void thrs_metod_mean_rdbx_CheckedChanged(object sender, EventArgs e)
        {
            customParams.Threshold.Method = "mean"; // Mean method seçildiğinde bu metodu kullanacak şekilde parametreyi ayarlıyorum
            UpdatePreviewImage();
        }


        private void otsu_thrshold_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (otsu_thrshold_rdbtn.Checked)
            {
                // ilgili parametre değiştiğinde otomatik görüntüye uygulatıp ekranda gösteriyorum ve alaksaız olan diğer parametrelerin görünürlüğünü güncelliyorum
                thrs_min_value_nud.Enabled = false;
                thrs_adptv_cValue_nud.Enabled = false;
                thrs_adptv_krnlSize_nud.Enabled = false;
                thrs_metod_gaussian_rdbx.Enabled = false; // adaptive threshold seçildiğinde diğer seçenekleri devre dışı bırakıyorum
                thrs_metod_mean_rdbx.Enabled = false;

                customParams.Threshold.Type = "otsu threshold";
                customParams.Threshold.MinThreshValue = null;
                customParams.Threshold.CValue = null;
                customParams.Threshold.KernelShape = null;


                UpdateListBox();
                UpdatePreviewImage();
            }
        }

        private void blr_gaussen_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (blr_gaussen_rdbtn.Checked)
            {
                // ilgili parametre değiştiğinde otomatik görüntüye uygulatıp ekranda gösteriyorum
                customParams.Bluring.Type = "gaussen";
                UpdateListBox();
                UpdatePreviewImage();
            }
        }

        private void blr_median_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (blr_median_rdbtn.Checked)
            {
                // ilgili parametre değiştiğinde otomatik görüntüye uygulatıp ekranda gösteriyorum
                customParams.Bluring.Type = "median";
                UpdateListBox();
                UpdatePreviewImage();
            }
        }

        private void blr_blur_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (blr_blur_rdbtn.Checked)
            {
                // ilgili parametre değiştiğinde otomatik görüntüye uygulatıp ekranda gösteriyorum
                customParams.Bluring.Type = "blur";
                UpdateListBox();
                UpdatePreviewImage();
            }
        }

        private void blr_filter_shape_nud_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre değiştiğinde otomatik görüntüye uygulatıp ekranda gösteriyorum
            customParams.Bluring.FilterShape = blr_filter_shape_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();
        }


        private void nextImg_btn_Click(object sender, EventArgs e)
        {
            // bir sonraki görüntü seçildiğinde imageindex'i artırıp ekranda güncelleme işlemi yapıyorum
            if (currentImg_picbox.Image != null)
            {
                if (currentImageIndex < ApprovedImages.Count - 1)
                {
                    currentImageIndex++;
                    zoomController.ResetZoom();
                    UpdatePreviewImage();
                }
            }
        }

        private void previousImg_btn_Click(object sender, EventArgs e)
        {
            // bir sonraki görüntü seçildiğinde imageindex'i azaltarak bir önceki resmi göstermesini sağlıyorum 
            if (currentImg_picbox.Image != null)
            {
                if (currentImageIndex > 0)
                {
                    currentImageIndex--;
                    zoomController.ResetZoom();
                    UpdatePreviewImage();

                }
            }
        }

        private void deletSlctdImg_Click(object sender, EventArgs e)
        {
            if (currentImg_picbox.Image != null & ApprovedImages.Count > 0)
            {
                zoomController.ResetZoom();
                ApprovedImages.RemoveAt(currentImageIndex);
                UnReadyImages.RemoveAt(currentImageIndex);

                // Eğer currentImageIndex artık liste sınırları dışındaysa bir geri git
                if (currentImageIndex >= ApprovedImages.Count)
                {
                    currentImageIndex = ApprovedImages.Count - 1;

                }
                // Liste boşaldıysa görüntüyü temizle
                if (ApprovedImages.Count == 0)
                {
                    currentImg_picbox.Image?.Dispose();
                    currentImg_picbox.Image = null;
                    currentImageIndex = 0;

                    unreadyImg_picbox.Image?.Dispose();
                    unreadyImg_picbox.Image = null;

                    approvedImg_picbox.Image?.Dispose();
                    approvedImg_picbox.Image = null;

                    onIsleme_combobx.SelectedIndex = -1; // Ön işleme combobox'ını sıfırla
                    setGroupBoxesDefault(); // Grup kutularını varsayılan duruma getir

                    return;
                }

                UpdatePreviewImage();

                Mat mixedImg = util.CombineImagesHorizontally(UnReadyImages);
                unreadyImg_picbox.Image = BitmapConverter.ToBitmap(mixedImg);

                Mat mixedImg2 = util.CombineImagesHorizontally(ApprovedImages);
                approvedImg_picbox.Image = BitmapConverter.ToBitmap(mixedImg2);

            }
        }

        private void clip_limit_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.Contrast.ClipLimit = clip_limit_nud.Value; // Tile grid size değiştiğinde parametreyi güncelle
            customParams.Contrast.Type = "Clahe"; // Contrast işlemi için adını güncelle

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void tile_grid_size_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.Contrast.GridSize = tile_grid_size_nud.Value; // Tile grid size değiştiğinde parametreyi güncelle
            customParams.Contrast.Type = "Clahe"; // Contrast işlemi için adını güncelle

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void edg_laplacian_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            edg_ksize_nud.Enabled = true; // Laplacian için kernel boyutunu etkinleştiriyorum

            edg_cannyMaxThrs_nud.Enabled = false; // Canny' e ait parametreleri devre dışı bırakıyorum
            edg_cannyMinThrs_nud.Enabled = false; // Canny' e ait parametreleri devre dışı bırakıyorum


            customParams.Edge.Type = "laplacian"; // Laplacian kenar algılama türünü ayarlıyorum


            UpdateListBox();
            UpdatePreviewImage();

        }

        private void edg_sobelX_rdbtn_CheckedChanged(object sender, EventArgs e)
        {

            edg_ksize_nud.Enabled = true; // sobelX için kernel boyutunu etkinleştiriyorum

            edg_cannyMaxThrs_nud.Enabled = false; // Canny' e ait parametreleri devre dışı bırakıyorum
            edg_cannyMinThrs_nud.Enabled = false; // Canny' e ait parametreleri devre dışı bırakıyorum


            customParams.Edge.Type = "sobelX"; // sobelX kenar algılama türünü ayarlıyorum

            UpdateListBox();
            UpdatePreviewImage();

        }

        private void edg_sobelY_rdbtn_CheckedChanged(object sender, EventArgs e)
        {

            edg_ksize_nud.Enabled = true; // sobelY için kernel boyutunu etkinleştiriyorum

            edg_cannyMaxThrs_nud.Enabled = false; // Canny' e ait parametreleri devre dışı bırakıyorum
            edg_cannyMinThrs_nud.Enabled = false; // Canny' e ait parametreleri devre dışı bırakıyorum


            customParams.Edge.Type = "sobelY"; // sobelY kenar algılama türünü ayarlıyorum

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void edg_sobel_rdbtn_CheckedChanged(object sender, EventArgs e)
        {

            edg_ksize_nud.Enabled = true; // sobel için kernel boyutunu etkinleştiriyorum

            edg_cannyMaxThrs_nud.Enabled = false; // Canny' e ait parametreleri devre dışı bırakıyorum
            edg_cannyMinThrs_nud.Enabled = false; // Canny' e ait parametreleri devre dışı bırakıyorum


            customParams.Edge.Type = "sobel"; // sobel kenar algılama türünü ayarlıyorum

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void edg_canny_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            edg_ksize_nud.Enabled = false; // Canny için kernel boyutunu devre dışı bırakıyorum
            edg_cannyMaxThrs_nud.Enabled = true; // Canny için maksimum eşik değerini etkinleştiriyorum
            edg_cannyMinThrs_nud.Enabled = true; // Canny için minimum eşik değerini etkinleştiriyorum

            customParams.Edge.Type = "canny"; // Canny kenar algılama türünü ayarlıyorum

            UpdateListBox();
            UpdatePreviewImage();

        }



        private void edg_ksize_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.Edge.KernelShape = edg_ksize_nud.Value; // Kernel boyutunu ayarlıyorum

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void edg_cannyMinThrs_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.Edge.MinThrashold = edg_cannyMinThrs_nud.Value; // Canny minimum eşik değerini ayarlıyorum

            UpdateListBox();
            UpdatePreviewImage();

        }

        private void edg_cannyMaxThrs_nud_ValueChanged(object sender, EventArgs e)
        {

            customParams.Edge.MaxThrashold = edg_cannyMaxThrs_nud.Value; // Canny maksimum eşik değerini ayarlıyorum

            UpdateListBox();
            UpdatePreviewImage();

        }

        private void stdHough_linep_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (stdHough_linep_rdbtn.Checked)
            {
                stdHoughP_grpbx.Enabled = true;
                stdHoughC_grpbx.Enabled = false;

                customParams.HoughTransform.Type = "houghP"; // Hough Transform türünü ayarlıyorum

                UpdateListBox();
                UpdatePreviewImage();
            }




        }


        private void stdHoughL_thrs_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.HoughTransform.Thrashold = stdHoughL_thrs_nud.Value; // Hough Transform için eşik değerini ayarlıyorum


            UpdateListBox();
            UpdatePreviewImage();
        }

        private void stdHoughL_minLine_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.HoughTransform.MinLineLenght = stdHoughL_minLine_nud.Value;

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void stdHoughL_maxLine_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.HoughTransform.MaxLineLenght = stdHoughL_maxLine_nud.Value;

            UpdateListBox();
            UpdatePreviewImage();

        }

        private void stdHough_circle_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (stdHough_circle_rdbtn.Checked)
            {
                // Hough Circle seçildiğinde ilgili grup kutusunu etkinleştiriyorum ve diğerini devre dışı bırakıyorum
                stdHoughP_grpbx.Enabled = false;
                stdHoughC_grpbx.Enabled = true;

                customParams.HoughTransform.Type = "houghC";

                UpdateListBox();
                UpdatePreviewImage();
            }
        }

        private void stdHoughC_dp_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.HoughTransform.Dp = stdHoughC_dp_nud.Value;

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void stdHoughC_minDistance_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.HoughTransform.MinDistance = stdHoughC_minDistance_nud.Value;

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void stdHoughC_param1_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.HoughTransform.Param1 = stdHoughC_param1_nud.Value;

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void stdHoughC_param2_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.HoughTransform.Param2 = stdHoughC_param2_nud.Value;

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void stdHoughC_minRadious_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.HoughTransform.MinRadious = stdHoughC_minRadious_nud.Value;

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void stdHoughC_maxRadious_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.HoughTransform.MaxRadious = stdHoughC_maxRadious_nud.Value;

            UpdateListBox();
            UpdatePreviewImage();
        }


    
    }
}  
