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
        // global deðiþkenleri tanýmlýyorum
        Utilities util = new Utilities();
        Mat img = new Mat();
        List<Mat> ApprovedImages = new List<Mat>(); // Kullanýcýnýn onayladýðý OCR için hazýr resimleri saklamak için liste
        List<Mat> UnReadyImages = new List<Mat>(); // Kullanýcýnýn seçtiði görselleri ekranda gösteren ancak ocr için hazýr olmayan resimleri saklamak için liste
        String OrcResult; // OCR sonuçlarýný saklamak için kullanýlan string deðiþken
        string preproccesMethod; //seçilen öniþleme adýmýna ait bilgiyi tutan deðiþken

        ParamOptimization paramOptimization = new ParamOptimization(); // ön iþleme adýmlarýný yaptýðým sýnýfýn örneði
        int currentImageIndex = 0; // öniþleme iþleminin yapýldýðý güncel görüntü indeksini tutan deðiþken
        CustomPreprocessParams customParams = new CustomPreprocessParams(); // öniþlemelere ait parametreleri tutan sýnýfýn örneði
        ImageZoomController zoomController;
        List<ICustomParmPreprocces> appliedSteps = new List<ICustomParmPreprocces>(); // Kullanýcýnýn onayladýðý ön iþleme adýmlarýný saklamak için list

        // RectangleSelector sýnýfý, kullanýcýlarýn resim üzerinde dikdörtgen seçim yapmasýna olanak tanýr.
        private RectangleSelector selector;
        public Form1()
        {
            InitializeComponent();

            // görüntü kutumun içindeki eventleri dinleyerek güncelliyorum
            selector = new RectangleSelector(upload_picBox);

            // Baþlangýç için default ön iþleme deðerlerini atýyoruz
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

                    KernelShape = edg_ksize_nud.Value, // Varsayýlan deðer

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
        /// <param name="img_info">Resim ile iliþkili bilgi (örneðin buton etiketi).</param>
        private void read_and_set_img(string img_path, string img_info)
        {
            // Seçilen resim dosyasýnýn yüklüyoruz
            img = util.setChosenImage(img_path);

            // Seçilen resim bilgisini güncelleme
            util.setImgBtnClick(img_info);

            // görüntüyü tam sýðdýmak için otomatik boyutlandýrma ve PictureBox'a atýyorum
            upload_picBox.SizeMode = PictureBoxSizeMode.Zoom; // Görüntüyü tam sýðdýrmak için otomatik boyutlandýrma
            upload_picBox.Image = BitmapConverter.ToBitmap(img);
        }


        /// <summary>
        /// Uygulanan ve geçici olan ön iþleme adýmlarýný ListBox'ta listeler.
        /// </summary>
        private void UpdateListBox()
        {
            listBox1.Items.Clear(); // Önce liste kutusunu temizle

            // Eðer kullanýcý tarafýndan onaylanan iþlemler varsa, bunlarý listeleriz
            if (appliedSteps.Any())
            {
                listBox1.Items.Add("Onaylanmýþ Yöntemler:");

                foreach (var step in appliedSteps)
                {
                    // Ýþlem adý ve türü ilk olarak yazdýrýlýr
                    listBox1.Items.Add($"Name: {step.Name}");
                    listBox1.Items.Add($"Type: {step.Type}");

                    // Diðer tüm özellikler (parametreler) yazdýrýlýr
                    // "Name" ve "Type" dýþýndakiler
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

                    listBox1.Items.Add(""); // Her adým sonunda boþ satýr býrak
                }
            }

            // Eðer kullanýcý bir iþlem seçmiþ ama onaylamamýþsa, geçici hali de gösterilir
            if (!string.IsNullOrEmpty(preproccesMethod))
            {
                var currentStep = customParams.GetByName(preproccesMethod);

                listBox1.Items.Add("Önizleme (Onaylanmadý):");

                if (currentStep != null)
                {
                    listBox1.Items.Add($"Name: {currentStep.Name}");
                    listBox1.Items.Add($"Type: {currentStep.Type}");

                    // Geçici adýmýn parametrelerini yazdýr
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
        /// Tüm ön iþleme parametrelerini varsayýlan deðerlere sýfýrlar.
        /// </summary>
        private void SetPrepreccesStepsDefault()
        {
            // ayarlarý varsayalýna dönüþtüyorum
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
        /// Onaylanmýþ görüntü üzerinden geçici ön iþleme adýmýný uygular ve sonucu görüntüler.
        /// </summary>
        private async void UpdatePreviewImage()
        {
            if (ApprovedImages.Count > 0)
            {
                // sonsuz bir döngü oluþturduðum için ekranýn kilitlenmemes için async fonksiyon kullandým. pictureBox'ý anlýk güncelliyorum.
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
            // Kullanýcýdan özel bir resim seçmesini istiyoruz
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.tiff",
                Title = "Select an Image File"
            };

            // eðer kullanýcý bir resim seçerse
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

            // Eðer görüntü seçiliyse
            if (secilenBtn != "none")
            {
                // kiþi resim üzerinde kýrpma iþlemi yapmýþsa bu görüntüyü kýrpýp dönderiyorum kýrpýlmamýþsa ayný görüntü dönderir.
                Mat processedImg = util.CropImage(img, selector);
                Mat rawCopy = processedImg.Clone(); // görselin kopyasýný alýyorum, böylece orijinal görüntüye dokunmadan iþlem yapabilirim

                // Onaylanan resimlerin listesini güncelliyorum
                util.AddApprovedImage(processedImg, ApprovedImages);
                // Onaylanan resimleri birleþtiriyorum ve 1 görsel elde ediyorum.
                Mat mixedImg = util.CombineImagesHorizontally(ApprovedImages);

                // görüntüyü tam sýðdýmak için otomatik boyutlandýrma ve PictureBox'a atýyorum
                approvedImg_picbox.SizeMode = PictureBoxSizeMode.Zoom;
                approvedImg_picbox.Image = BitmapConverter.ToBitmap(mixedImg);



                // ocr için hazýr olmayan görselleri ham görseli UnReadyImages listesine ekliyorum
                UnReadyImages.Add(rawCopy);
                // ocr için hazýr olmayan görselleri birleþtiriyorum ve 1 görsel elde ediyorum.
                Mat mixedImg2 = util.CombineImagesHorizontally(UnReadyImages);

                // görüntüyü tam sýðdýmak için otomatik boyutlandýrma ve PictureBox'a atýyorum
                unreadyImg_picbox.SizeMode = PictureBoxSizeMode.Zoom;
                unreadyImg_picbox.Image = BitmapConverter.ToBitmap(mixedImg2);




                // ilk baþta seçim yapýldýðýnda prewierImageBoxa atama yapýyorum
                currentImg_picbox.SizeMode = PictureBoxSizeMode.Zoom;
                currentImg_picbox.Image = BitmapConverter.ToBitmap(ApprovedImages[currentImageIndex]);



                // görüntü eklendiðinde upload_picBox otomatik sýfýrlansýn 
                upload_picBox.Image?.Dispose();
                upload_picBox.Image = null;
                util.setImgBtnClick("none");

            }
            // Eðer kullanýcý bir resim seçmemiþse, kullanýcýya bir mesaj gösteriyoruz
            else
            {
                MessageBox.Show("Lütfen bir resim seçin.");
            }
        }
        private void img_to_text_btn_Click(object sender, EventArgs e)
        {
            // her OCR iþlemi için bu butona basýldýðýnda, sonuçlarý temizleyerek yeni görüntülerden elde edilen metinleri temiz þekilde baþlatýyoruz
            result_txt_box.Text = "";

            // Seçilen resme ait bilgiyi alýyoruz
            string secilenBtn = util.getImgBtnClicked();

            // Eðer kullanýcý herhangi bir resmi onaylamadýysa kullanýcýya bir mesaj gösteriyoruz
            if (ApprovedImages.Count == 0)
            {
                MessageBox.Show("Lütfen bir resim seçin ve onaylayýn.");
                return; // Eðer kullanýcý bir resim seçmemiþse, iþlemi sonlandýrýyoruz
            }

            // Eðer kullanýcý resim onayladýysa, onaylanan resimler üzerinde iþlemler yaparak OCR  çýktýsýný alýyoruz
            else
            {
                String text;

                // Onaylanan her bir görüntü için
                for (int i = 0; i < ApprovedImages.Count; i++)
                {
                    // Onaylanan her resim ve ona ait kaynak bilgilerini alýyoruz
                    var currentImg = ApprovedImages[i];


                    OcrResult ocrResult = new OcrResult();
                    //OCR sonuçlarýný metin olarak alýyoruz.

                    text = ocrResult.GetOcrResult(currentImg);

                    // eðer birden fazla görüntüden metin alýndýysa, metinleri birleþtiriyoruz

                    OrcResult += "------------------------Bir Sonraki Resim-----------------------\n" + text;
                }
            }

            result_txt_box.Text = OrcResult; // Ýþlenmiþ metni TextBox'a yazdýr
            OrcResult = ""; // OCR sonuçlarýný temizle. Yeni bir iþlem yaptýðýnda var olan metinlerin üzerine yazýlmasýný engellemek için.
        }
        private void onIsleme_combobx_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (onIsleme_combobx.SelectedIndex == -1)
            {
                return;
            }

            if (ApprovedImages.Count > 0)
            {
                // seçilen ön iþleme adýmýný al
                preproccesMethod = onIsleme_combobx.SelectedItem?.ToString();

                // Tüm grup kutularýný gizle (önce sýfýrla)
                setGroupBoxesDefault();

                // Onayla butonunu göster ve yerini ayarla (default)
                onisleme_kydt_btn.Visible = true;

                // Öniþleme adýmýna göre ayarlamalar
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
                MessageBox.Show("Lütfen Custom Resim Seçin ve Onaylayýn");
            }

        }


        private void onisleme_kydt_btn_Click_1(object sender, EventArgs e)
        {
            // kaydet butonuna basýldýðýnda ekrandaki bilglileri alýp atama iþlemi yapar ve appliedSteps dizimin içerisinde onaylanan her bir ön iþleme adýmýný tutar.
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
                    MessageBox.Show("Lütfen geçerli bir iþlem seçiniz.");
                    return;
                }



                // Seçilen öniþleme adýmýný alýyorum
                var step = customParams.GetByName(preproccesMethod);
                // Eðer listede görüntü varsa, o görüntüyü alýp onun üzerine iþlem yapýyorum
                if (ApprovedImages.Count > 0)
                {

                    // Mevcut görüntünün referansýný al
                    var currentImage = ApprovedImages[currentImageIndex];

                    // Ýþlemi uygula (referansla)
                    paramOptimization.ApplyStep(step, ref currentImage);

                    // Listeyi güncelle (gerekirse)
                    ApprovedImages[currentImageIndex] = currentImage;
                }



                // Preview baþlat
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
                // her  onayla iþleminden sonra ön iþleme parametrelerini varsayýlana dönderiyiorum
                SetPrepreccesStepsDefault();
            }
            else
            {
                MessageBox.Show("Lütfen Resim Seçin ve Onaylayýn");
            }

        }


        private void cln_img_Click(object sender, EventArgs e)
        {
            ApprovedImages.Clear(); // Kýrpýlmýþ resimleri temizle
            UnReadyImages.Clear(); // Onaylanmamýþ resimleri temizle
            upload_picBox.Image = null; // yüklenen resmi temizle
            unreadyImg_picbox.Image = null; // onaylanan resmi temizle
            result_txt_box.Text = ""; // TextBox'taki metni temizle
            OrcResult = ""; // OCR sonuçlarýný temizle
            util.setImgBtnClick("none"); // Seçilen resim butonunu ait bilgiyi güncelliyoruz
            currentImg_picbox.Image = null; // Preview görüntüsünü temizle
            approvedImg_picbox.Image = null;
            onIsleme_combobx.SelectedIndex = -1; // Ön iþleme combobox'ýný sýfýrla
            currentImageIndex = 0;
            setGroupBoxesDefault(); // Grup kutularýný varsayýlana döndür
            SetPrepreccesStepsDefault();

        }



        private void mrflj_fltr_wdth_nud_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre deðiþtiðinde otomatik görüntüye uygulatýp ekranda gösteriyorum
            customParams.Morphological.FilterW = mrflj_fltr_wdth_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();

        }


        private async void mrflj_iteration_nud_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre deðiþtiðinde otomatik görüntüye uygulatýp ekranda gösteriyorum
            customParams.Morphological.Iterations = mrflj_iteration_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();

        }

        private async void mrflj_fltr_hgt_nud_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre deðiþtiðinde otomatik görüntüye uygulatýp ekranda gösteriyorum
            customParams.Morphological.FilterH = mrflj_fltr_hgt_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();

        }

        private async void mrflj_erodion_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (mrflj_erodion_rdbtn.Checked)
            {
                // ilgili parametre deðiþtiðinde otomatik görüntüye uygulatýp ekranda gösteriyorum
                customParams.Morphological.Type = "erodion";
                UpdateListBox();
                UpdatePreviewImage();

            }
        }

        private async void mrflj_dilation_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (mrflj_dilation_rdbtn.Checked)
            {
                // ilgili parametre deðiþtiðinde otomatik görüntüye uygulatýp ekranda gösteriyorum
                customParams.Morphological.Type = "dilation";
                UpdateListBox();
                UpdatePreviewImage();

            }
        }

        private async void mrflj_opening_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (mrflj_opening_rdbtn.Checked)
            {
                // ilgili parametre deðiþtiðinde otomatik görüntüye uygulatýp ekranda gösteriyorum
                customParams.Morphological.Type = "opening";
                UpdateListBox();
                UpdatePreviewImage();

            }
        }

        private async void mrflj_closing_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (mrflj_closing_rdbtn.Checked)
            {
                // ilgili parametre deðiþtiðinde otomatik görüntüye uygulatýp ekranda gösteriyorum
                customParams.Morphological.Type = "closing";
                UpdateListBox();
                UpdatePreviewImage();

            }
        }

        private void thrs_adptv_krnlSize_nud_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre deðiþtiðinde otomatik görüntüye uygulatýp ekranda gösteriyorum
            customParams.Threshold.KernelShape = thrs_adptv_krnlSize_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();

        }

        private void thrs_adptv_cValue_nud_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre deðiþtiðinde otomatik görüntüye uygulatýp ekranda gösteriyorum
            customParams.Threshold.CValue = thrs_adptv_cValue_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre deðiþtiðinde otomatik görüntüye uygulatýp ekranda gösteriyorum
            customParams.Threshold.MinThreshValue = thrs_min_value_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();
        }

        private void thrshold_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (thrshold_rdbtn.Checked)
            {
                // ilgili parametre deðiþtiðinde otomatik görüntüye uygulatýp ekranda gösteriyorum ve alaksaýz olan diðer parametrelerin görünürlüðünü güncelliyorum
                thrs_min_value_nud.Enabled = true;
                thrs_adptv_cValue_nud.Enabled = false;
                thrs_adptv_krnlSize_nud.Enabled = false;
                thrs_metod_gaussian_rdbx.Enabled = false; // adaptive threshold seçildiðinde diðer seçenekleri devre dýþý býrakýyorum
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
                // ilgili parametre deðiþtiðinde otomatik görüntüye uygulatýp ekranda gösteriyorum ve alaksaýz olan diðer parametrelerin görünürlüðünü güncelliyorum
                thrs_min_value_nud.Enabled = false;
                thrs_adptv_cValue_nud.Enabled = true;
                thrs_adptv_krnlSize_nud.Enabled = true;
                thrs_metod_gaussian_rdbx.Enabled = true; // adaptive threshold seçildiðinde diðer seçenekleri devre dýþý býrakýyorum
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
            customParams.Threshold.Isbitwise = bitwise_thrshold_chckBx.Checked; // Bitwise threshold seçildiðinde bu metodu kullanacak þekilde parametreyi ayarlýyorum
            UpdatePreviewImage();
        }

        private void thrs_metod_gaussian_rdbx_CheckedChanged(object sender, EventArgs e)
        {
            customParams.Threshold.Method = "gaussian"; // Gaussian method seçildiðinde bu metodu kullanacak þekilde parametreyi ayarlýyorum
            UpdatePreviewImage();
        }

        private void thrs_metod_mean_rdbx_CheckedChanged(object sender, EventArgs e)
        {
            customParams.Threshold.Method = "mean"; // Mean method seçildiðinde bu metodu kullanacak þekilde parametreyi ayarlýyorum
            UpdatePreviewImage();
        }


        private void otsu_thrshold_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (otsu_thrshold_rdbtn.Checked)
            {
                // ilgili parametre deðiþtiðinde otomatik görüntüye uygulatýp ekranda gösteriyorum ve alaksaýz olan diðer parametrelerin görünürlüðünü güncelliyorum
                thrs_min_value_nud.Enabled = false;
                thrs_adptv_cValue_nud.Enabled = false;
                thrs_adptv_krnlSize_nud.Enabled = false;
                thrs_metod_gaussian_rdbx.Enabled = false; // adaptive threshold seçildiðinde diðer seçenekleri devre dýþý býrakýyorum
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
                // ilgili parametre deðiþtiðinde otomatik görüntüye uygulatýp ekranda gösteriyorum
                customParams.Bluring.Type = "gaussen";
                UpdateListBox();
                UpdatePreviewImage();
            }
        }

        private void blr_median_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (blr_median_rdbtn.Checked)
            {
                // ilgili parametre deðiþtiðinde otomatik görüntüye uygulatýp ekranda gösteriyorum
                customParams.Bluring.Type = "median";
                UpdateListBox();
                UpdatePreviewImage();
            }
        }

        private void blr_blur_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (blr_blur_rdbtn.Checked)
            {
                // ilgili parametre deðiþtiðinde otomatik görüntüye uygulatýp ekranda gösteriyorum
                customParams.Bluring.Type = "blur";
                UpdateListBox();
                UpdatePreviewImage();
            }
        }

        private void blr_filter_shape_nud_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre deðiþtiðinde otomatik görüntüye uygulatýp ekranda gösteriyorum
            customParams.Bluring.FilterShape = blr_filter_shape_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();
        }


        private void nextImg_btn_Click(object sender, EventArgs e)
        {
            // bir sonraki görüntü seçildiðinde imageindex'i artýrýp ekranda güncelleme iþlemi yapýyorum
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
            // bir sonraki görüntü seçildiðinde imageindex'i azaltarak bir önceki resmi göstermesini saðlýyorum 
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

                // Eðer currentImageIndex artýk liste sýnýrlarý dýþýndaysa bir geri git
                if (currentImageIndex >= ApprovedImages.Count)
                {
                    currentImageIndex = ApprovedImages.Count - 1;

                }
                // Liste boþaldýysa görüntüyü temizle
                if (ApprovedImages.Count == 0)
                {
                    currentImg_picbox.Image?.Dispose();
                    currentImg_picbox.Image = null;
                    currentImageIndex = 0;

                    unreadyImg_picbox.Image?.Dispose();
                    unreadyImg_picbox.Image = null;

                    approvedImg_picbox.Image?.Dispose();
                    approvedImg_picbox.Image = null;

                    onIsleme_combobx.SelectedIndex = -1; // Ön iþleme combobox'ýný sýfýrla
                    setGroupBoxesDefault(); // Grup kutularýný varsayýlan duruma getir

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
            customParams.Contrast.ClipLimit = clip_limit_nud.Value; // Tile grid size deðiþtiðinde parametreyi güncelle
            customParams.Contrast.Type = "Clahe"; // Contrast iþlemi için adýný güncelle

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void tile_grid_size_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.Contrast.GridSize = tile_grid_size_nud.Value; // Tile grid size deðiþtiðinde parametreyi güncelle
            customParams.Contrast.Type = "Clahe"; // Contrast iþlemi için adýný güncelle

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void edg_laplacian_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            edg_ksize_nud.Enabled = true; // Laplacian için kernel boyutunu etkinleþtiriyorum

            edg_cannyMaxThrs_nud.Enabled = false; // Canny' e ait parametreleri devre dýþý býrakýyorum
            edg_cannyMinThrs_nud.Enabled = false; // Canny' e ait parametreleri devre dýþý býrakýyorum


            customParams.Edge.Type = "laplacian"; // Laplacian kenar algýlama türünü ayarlýyorum


            UpdateListBox();
            UpdatePreviewImage();

        }

        private void edg_sobelX_rdbtn_CheckedChanged(object sender, EventArgs e)
        {

            edg_ksize_nud.Enabled = true; // sobelX için kernel boyutunu etkinleþtiriyorum

            edg_cannyMaxThrs_nud.Enabled = false; // Canny' e ait parametreleri devre dýþý býrakýyorum
            edg_cannyMinThrs_nud.Enabled = false; // Canny' e ait parametreleri devre dýþý býrakýyorum


            customParams.Edge.Type = "sobelX"; // sobelX kenar algýlama türünü ayarlýyorum

            UpdateListBox();
            UpdatePreviewImage();

        }

        private void edg_sobelY_rdbtn_CheckedChanged(object sender, EventArgs e)
        {

            edg_ksize_nud.Enabled = true; // sobelY için kernel boyutunu etkinleþtiriyorum

            edg_cannyMaxThrs_nud.Enabled = false; // Canny' e ait parametreleri devre dýþý býrakýyorum
            edg_cannyMinThrs_nud.Enabled = false; // Canny' e ait parametreleri devre dýþý býrakýyorum


            customParams.Edge.Type = "sobelY"; // sobelY kenar algýlama türünü ayarlýyorum

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void edg_sobel_rdbtn_CheckedChanged(object sender, EventArgs e)
        {

            edg_ksize_nud.Enabled = true; // sobel için kernel boyutunu etkinleþtiriyorum

            edg_cannyMaxThrs_nud.Enabled = false; // Canny' e ait parametreleri devre dýþý býrakýyorum
            edg_cannyMinThrs_nud.Enabled = false; // Canny' e ait parametreleri devre dýþý býrakýyorum


            customParams.Edge.Type = "sobel"; // sobel kenar algýlama türünü ayarlýyorum

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void edg_canny_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            edg_ksize_nud.Enabled = false; // Canny için kernel boyutunu devre dýþý býrakýyorum
            edg_cannyMaxThrs_nud.Enabled = true; // Canny için maksimum eþik deðerini etkinleþtiriyorum
            edg_cannyMinThrs_nud.Enabled = true; // Canny için minimum eþik deðerini etkinleþtiriyorum

            customParams.Edge.Type = "canny"; // Canny kenar algýlama türünü ayarlýyorum

            UpdateListBox();
            UpdatePreviewImage();

        }



        private void edg_ksize_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.Edge.KernelShape = edg_ksize_nud.Value; // Kernel boyutunu ayarlýyorum

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void edg_cannyMinThrs_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.Edge.MinThrashold = edg_cannyMinThrs_nud.Value; // Canny minimum eþik deðerini ayarlýyorum

            UpdateListBox();
            UpdatePreviewImage();

        }

        private void edg_cannyMaxThrs_nud_ValueChanged(object sender, EventArgs e)
        {

            customParams.Edge.MaxThrashold = edg_cannyMaxThrs_nud.Value; // Canny maksimum eþik deðerini ayarlýyorum

            UpdateListBox();
            UpdatePreviewImage();

        }

        private void stdHough_linep_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (stdHough_linep_rdbtn.Checked)
            {
                stdHoughP_grpbx.Enabled = true;
                stdHoughC_grpbx.Enabled = false;

                customParams.HoughTransform.Type = "houghP"; // Hough Transform türünü ayarlýyorum

                UpdateListBox();
                UpdatePreviewImage();
            }




        }


        private void stdHoughL_thrs_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.HoughTransform.Thrashold = stdHoughL_thrs_nud.Value; // Hough Transform için eþik deðerini ayarlýyorum


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
                // Hough Circle seçildiðinde ilgili grup kutusunu etkinleþtiriyorum ve diðerini devre dýþý býrakýyorum
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