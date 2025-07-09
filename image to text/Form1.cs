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
        // global de�i�kenleri tan�ml�yorum
        Utilities util = new Utilities();
        Mat img = new Mat();
        List<Mat> ApprovedImages = new List<Mat>(); // Kullan�c�n�n onaylad��� OCR i�in haz�r resimleri saklamak i�in liste
        List<Mat> UnReadyImages = new List<Mat>(); // Kullan�c�n�n se�ti�i g�rselleri ekranda g�steren ancak ocr i�in haz�r olmayan resimleri saklamak i�in liste
        String OrcResult; // OCR sonu�lar�n� saklamak i�in kullan�lan string de�i�ken
        string preproccesMethod; //se�ilen �ni�leme ad�m�na ait bilgiyi tutan de�i�ken

        ParamOptimization paramOptimization = new ParamOptimization(); // �n i�leme ad�mlar�n� yapt���m s�n�f�n �rne�i
        int currentImageIndex = 0; // �ni�leme i�leminin yap�ld��� g�ncel g�r�nt� indeksini tutan de�i�ken
        CustomPreprocessParams customParams = new CustomPreprocessParams(); // �ni�lemelere ait parametreleri tutan s�n�f�n �rne�i
        ImageZoomController zoomController;
        List<ICustomParmPreprocces> appliedSteps = new List<ICustomParmPreprocces>(); // Kullan�c�n�n onaylad��� �n i�leme ad�mlar�n� saklamak i�in list

        // RectangleSelector s�n�f�, kullan�c�lar�n resim �zerinde dikd�rtgen se�im yapmas�na olanak tan�r.
        private RectangleSelector selector;
        public Form1()
        {
            InitializeComponent();

            // g�r�nt� kutumun i�indeki eventleri dinleyerek g�ncelliyorum
            selector = new RectangleSelector(upload_picBox);

            // Ba�lang�� i�in default �n i�leme de�erlerini at�yoruz
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

                    KernelShape = edg_ksize_nud.Value, // Varsay�lan de�er

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
        /// Verilen resim yolundan resmi okuyup y�kler, ilgili g�rsel bilgilerini g�nceller
        /// ve PictureBox'ta g�r�nt�ler.
        /// </summary>
        /// <param name="img_path">Y�klenecek resmin dosya yolu.</param>
        /// <param name="img_info">Resim ile ili�kili bilgi (�rne�in buton etiketi).</param>
        private void read_and_set_img(string img_path, string img_info)
        {
            // Se�ilen resim dosyas�n�n y�kl�yoruz
            img = util.setChosenImage(img_path);

            // Se�ilen resim bilgisini g�ncelleme
            util.setImgBtnClick(img_info);

            // g�r�nt�y� tam s��d�mak i�in otomatik boyutland�rma ve PictureBox'a at�yorum
            upload_picBox.SizeMode = PictureBoxSizeMode.Zoom; // G�r�nt�y� tam s��d�rmak i�in otomatik boyutland�rma
            upload_picBox.Image = BitmapConverter.ToBitmap(img);
        }


        /// <summary>
        /// Uygulanan ve ge�ici olan �n i�leme ad�mlar�n� ListBox'ta listeler.
        /// </summary>
        private void UpdateListBox()
        {
            listBox1.Items.Clear(); // �nce liste kutusunu temizle

            // E�er kullan�c� taraf�ndan onaylanan i�lemler varsa, bunlar� listeleriz
            if (appliedSteps.Any())
            {
                listBox1.Items.Add("Onaylanm�� Y�ntemler:");

                foreach (var step in appliedSteps)
                {
                    // ��lem ad� ve t�r� ilk olarak yazd�r�l�r
                    listBox1.Items.Add($"Name: {step.Name}");
                    listBox1.Items.Add($"Type: {step.Type}");

                    // Di�er t�m �zellikler (parametreler) yazd�r�l�r
                    // "Name" ve "Type" d���ndakiler
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

                    listBox1.Items.Add(""); // Her ad�m sonunda bo� sat�r b�rak
                }
            }

            // E�er kullan�c� bir i�lem se�mi� ama onaylamam��sa, ge�ici hali de g�sterilir
            if (!string.IsNullOrEmpty(preproccesMethod))
            {
                var currentStep = customParams.GetByName(preproccesMethod);

                listBox1.Items.Add("�nizleme (Onaylanmad�):");

                if (currentStep != null)
                {
                    listBox1.Items.Add($"Name: {currentStep.Name}");
                    listBox1.Items.Add($"Type: {currentStep.Type}");

                    // Ge�ici ad�m�n parametrelerini yazd�r
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
        /// T�m �n i�leme parametrelerini varsay�lan de�erlere s�f�rlar.
        /// </summary>
        private void SetPrepreccesStepsDefault()
        {
            // ayarlar� varsayal�na d�n��t�yorum
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
        /// Onaylanm�� g�r�nt� �zerinden ge�ici �n i�leme ad�m�n� uygular ve sonucu g�r�nt�ler.
        /// </summary>
        private async void UpdatePreviewImage()
        {
            if (ApprovedImages.Count > 0)
            {
                // sonsuz bir d�ng� olu�turdu�um i�in ekran�n kilitlenmemes i�in async fonksiyon kulland�m. pictureBox'� anl�k g�ncelliyorum.
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
            // Kullan�c�dan �zel bir resim se�mesini istiyoruz
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.tiff",
                Title = "Select an Image File"
            };

            // e�er kullan�c� bir resim se�erse
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // g�r�nt�n�n yolunu belirliyoruz.
                string imgPath = openFileDialog.FileName;
                read_and_set_img(imgPath, "custom");

            }
        }
        private void onayla_btn_Click(object sender, EventArgs e)
        {
            // se�ilen resim butonuna ait bilgiyi al
            string secilenBtn = util.getImgBtnClicked();

            // E�er g�r�nt� se�iliyse
            if (secilenBtn != "none")
            {
                // ki�i resim �zerinde k�rpma i�lemi yapm��sa bu g�r�nt�y� k�rp�p d�nderiyorum k�rp�lmam��sa ayn� g�r�nt� d�nderir.
                Mat processedImg = util.CropImage(img, selector);
                Mat rawCopy = processedImg.Clone(); // g�rselin kopyas�n� al�yorum, b�ylece orijinal g�r�nt�ye dokunmadan i�lem yapabilirim

                // Onaylanan resimlerin listesini g�ncelliyorum
                util.AddApprovedImage(processedImg, ApprovedImages);
                // Onaylanan resimleri birle�tiriyorum ve 1 g�rsel elde ediyorum.
                Mat mixedImg = util.CombineImagesHorizontally(ApprovedImages);

                // g�r�nt�y� tam s��d�mak i�in otomatik boyutland�rma ve PictureBox'a at�yorum
                approvedImg_picbox.SizeMode = PictureBoxSizeMode.Zoom;
                approvedImg_picbox.Image = BitmapConverter.ToBitmap(mixedImg);



                // ocr i�in haz�r olmayan g�rselleri ham g�rseli UnReadyImages listesine ekliyorum
                UnReadyImages.Add(rawCopy);
                // ocr i�in haz�r olmayan g�rselleri birle�tiriyorum ve 1 g�rsel elde ediyorum.
                Mat mixedImg2 = util.CombineImagesHorizontally(UnReadyImages);

                // g�r�nt�y� tam s��d�mak i�in otomatik boyutland�rma ve PictureBox'a at�yorum
                unreadyImg_picbox.SizeMode = PictureBoxSizeMode.Zoom;
                unreadyImg_picbox.Image = BitmapConverter.ToBitmap(mixedImg2);




                // ilk ba�ta se�im yap�ld���nda prewierImageBoxa atama yap�yorum
                currentImg_picbox.SizeMode = PictureBoxSizeMode.Zoom;
                currentImg_picbox.Image = BitmapConverter.ToBitmap(ApprovedImages[currentImageIndex]);



                // g�r�nt� eklendi�inde upload_picBox otomatik s�f�rlans�n 
                upload_picBox.Image?.Dispose();
                upload_picBox.Image = null;
                util.setImgBtnClick("none");

            }
            // E�er kullan�c� bir resim se�memi�se, kullan�c�ya bir mesaj g�steriyoruz
            else
            {
                MessageBox.Show("L�tfen bir resim se�in.");
            }
        }
        private void img_to_text_btn_Click(object sender, EventArgs e)
        {
            // her OCR i�lemi i�in bu butona bas�ld���nda, sonu�lar� temizleyerek yeni g�r�nt�lerden elde edilen metinleri temiz �ekilde ba�lat�yoruz
            result_txt_box.Text = "";

            // Se�ilen resme ait bilgiyi al�yoruz
            string secilenBtn = util.getImgBtnClicked();

            // E�er kullan�c� herhangi bir resmi onaylamad�ysa kullan�c�ya bir mesaj g�steriyoruz
            if (ApprovedImages.Count == 0)
            {
                MessageBox.Show("L�tfen bir resim se�in ve onaylay�n.");
                return; // E�er kullan�c� bir resim se�memi�se, i�lemi sonland�r�yoruz
            }

            // E�er kullan�c� resim onaylad�ysa, onaylanan resimler �zerinde i�lemler yaparak OCR  ��kt�s�n� al�yoruz
            else
            {
                String text;

                // Onaylanan her bir g�r�nt� i�in
                for (int i = 0; i < ApprovedImages.Count; i++)
                {
                    // Onaylanan her resim ve ona ait kaynak bilgilerini al�yoruz
                    var currentImg = ApprovedImages[i];


                    OcrResult ocrResult = new OcrResult();
                    //OCR sonu�lar�n� metin olarak al�yoruz.

                    text = ocrResult.GetOcrResult(currentImg);

                    // e�er birden fazla g�r�nt�den metin al�nd�ysa, metinleri birle�tiriyoruz

                    OrcResult += "------------------------Bir Sonraki Resim-----------------------\n" + text;
                }
            }

            result_txt_box.Text = OrcResult; // ��lenmi� metni TextBox'a yazd�r
            OrcResult = ""; // OCR sonu�lar�n� temizle. Yeni bir i�lem yapt���nda var olan metinlerin �zerine yaz�lmas�n� engellemek i�in.
        }
        private void onIsleme_combobx_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (onIsleme_combobx.SelectedIndex == -1)
            {
                return;
            }

            if (ApprovedImages.Count > 0)
            {
                // se�ilen �n i�leme ad�m�n� al
                preproccesMethod = onIsleme_combobx.SelectedItem?.ToString();

                // T�m grup kutular�n� gizle (�nce s�f�rla)
                setGroupBoxesDefault();

                // Onayla butonunu g�ster ve yerini ayarla (default)
                onisleme_kydt_btn.Visible = true;

                // �ni�leme ad�m�na g�re ayarlamalar
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

                // �nizleme ve liste g�ncelle
                UpdatePreviewImage();
                UpdateListBox();
            }
            else
            {
                MessageBox.Show("L�tfen Custom Resim Se�in ve Onaylay�n");
            }

        }


        private void onisleme_kydt_btn_Click_1(object sender, EventArgs e)
        {
            // kaydet butonuna bas�ld���nda ekrandaki bilglileri al�p atama i�lemi yapar ve appliedSteps dizimin i�erisinde onaylanan her bir �n i�leme ad�m�n� tutar.
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
                    MessageBox.Show("L�tfen ge�erli bir i�lem se�iniz.");
                    return;
                }



                // Se�ilen �ni�leme ad�m�n� al�yorum
                var step = customParams.GetByName(preproccesMethod);
                // E�er listede g�r�nt� varsa, o g�r�nt�y� al�p onun �zerine i�lem yap�yorum
                if (ApprovedImages.Count > 0)
                {

                    // Mevcut g�r�nt�n�n referans�n� al
                    var currentImage = ApprovedImages[currentImageIndex];

                    // ��lemi uygula (referansla)
                    paramOptimization.ApplyStep(step, ref currentImage);

                    // Listeyi g�ncelle (gerekirse)
                    ApprovedImages[currentImageIndex] = currentImage;
                }



                // Preview ba�lat
                Bitmap previewImage = paramOptimization.StartPreviewFrame(ApprovedImages[currentImageIndex], preproccesMethod, customParams);

                // UI thread'inde g�ncelle
                currentImg_picbox.Image?.Dispose();
                currentImg_picbox.SizeMode = PictureBoxSizeMode.Zoom;
                currentImg_picbox.Image = previewImage;



                // onaylanan picBox'u g�ncelliyorum
                Mat mixedImg = util.CombineImagesHorizontally(ApprovedImages);

                approvedImg_picbox.Image?.Dispose();
                approvedImg_picbox.SizeMode = PictureBoxSizeMode.Zoom;
                approvedImg_picbox.Image = BitmapConverter.ToBitmap(mixedImg);

                UpdateListBox(); // Listeyi g�ncelle
                // her  onayla i�leminden sonra �n i�leme parametrelerini varsay�lana d�nderiyiorum
                SetPrepreccesStepsDefault();
            }
            else
            {
                MessageBox.Show("L�tfen Resim Se�in ve Onaylay�n");
            }

        }


        private void cln_img_Click(object sender, EventArgs e)
        {
            ApprovedImages.Clear(); // K�rp�lm�� resimleri temizle
            UnReadyImages.Clear(); // Onaylanmam�� resimleri temizle
            upload_picBox.Image = null; // y�klenen resmi temizle
            unreadyImg_picbox.Image = null; // onaylanan resmi temizle
            result_txt_box.Text = ""; // TextBox'taki metni temizle
            OrcResult = ""; // OCR sonu�lar�n� temizle
            util.setImgBtnClick("none"); // Se�ilen resim butonunu ait bilgiyi g�ncelliyoruz
            currentImg_picbox.Image = null; // Preview g�r�nt�s�n� temizle
            approvedImg_picbox.Image = null;
            onIsleme_combobx.SelectedIndex = -1; // �n i�leme combobox'�n� s�f�rla
            currentImageIndex = 0;
            setGroupBoxesDefault(); // Grup kutular�n� varsay�lana d�nd�r
            SetPrepreccesStepsDefault();

        }



        private void mrflj_fltr_wdth_nud_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre de�i�ti�inde otomatik g�r�nt�ye uygulat�p ekranda g�steriyorum
            customParams.Morphological.FilterW = mrflj_fltr_wdth_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();

        }


        private async void mrflj_iteration_nud_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre de�i�ti�inde otomatik g�r�nt�ye uygulat�p ekranda g�steriyorum
            customParams.Morphological.Iterations = mrflj_iteration_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();

        }

        private async void mrflj_fltr_hgt_nud_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre de�i�ti�inde otomatik g�r�nt�ye uygulat�p ekranda g�steriyorum
            customParams.Morphological.FilterH = mrflj_fltr_hgt_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();

        }

        private async void mrflj_erodion_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (mrflj_erodion_rdbtn.Checked)
            {
                // ilgili parametre de�i�ti�inde otomatik g�r�nt�ye uygulat�p ekranda g�steriyorum
                customParams.Morphological.Type = "erodion";
                UpdateListBox();
                UpdatePreviewImage();

            }
        }

        private async void mrflj_dilation_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (mrflj_dilation_rdbtn.Checked)
            {
                // ilgili parametre de�i�ti�inde otomatik g�r�nt�ye uygulat�p ekranda g�steriyorum
                customParams.Morphological.Type = "dilation";
                UpdateListBox();
                UpdatePreviewImage();

            }
        }

        private async void mrflj_opening_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (mrflj_opening_rdbtn.Checked)
            {
                // ilgili parametre de�i�ti�inde otomatik g�r�nt�ye uygulat�p ekranda g�steriyorum
                customParams.Morphological.Type = "opening";
                UpdateListBox();
                UpdatePreviewImage();

            }
        }

        private async void mrflj_closing_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (mrflj_closing_rdbtn.Checked)
            {
                // ilgili parametre de�i�ti�inde otomatik g�r�nt�ye uygulat�p ekranda g�steriyorum
                customParams.Morphological.Type = "closing";
                UpdateListBox();
                UpdatePreviewImage();

            }
        }

        private void thrs_adptv_krnlSize_nud_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre de�i�ti�inde otomatik g�r�nt�ye uygulat�p ekranda g�steriyorum
            customParams.Threshold.KernelShape = thrs_adptv_krnlSize_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();

        }

        private void thrs_adptv_cValue_nud_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre de�i�ti�inde otomatik g�r�nt�ye uygulat�p ekranda g�steriyorum
            customParams.Threshold.CValue = thrs_adptv_cValue_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre de�i�ti�inde otomatik g�r�nt�ye uygulat�p ekranda g�steriyorum
            customParams.Threshold.MinThreshValue = thrs_min_value_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();
        }

        private void thrshold_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (thrshold_rdbtn.Checked)
            {
                // ilgili parametre de�i�ti�inde otomatik g�r�nt�ye uygulat�p ekranda g�steriyorum ve alaksa�z olan di�er parametrelerin g�r�n�rl���n� g�ncelliyorum
                thrs_min_value_nud.Enabled = true;
                thrs_adptv_cValue_nud.Enabled = false;
                thrs_adptv_krnlSize_nud.Enabled = false;
                thrs_metod_gaussian_rdbx.Enabled = false; // adaptive threshold se�ildi�inde di�er se�enekleri devre d��� b�rak�yorum
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
                // ilgili parametre de�i�ti�inde otomatik g�r�nt�ye uygulat�p ekranda g�steriyorum ve alaksa�z olan di�er parametrelerin g�r�n�rl���n� g�ncelliyorum
                thrs_min_value_nud.Enabled = false;
                thrs_adptv_cValue_nud.Enabled = true;
                thrs_adptv_krnlSize_nud.Enabled = true;
                thrs_metod_gaussian_rdbx.Enabled = true; // adaptive threshold se�ildi�inde di�er se�enekleri devre d��� b�rak�yorum
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
            customParams.Threshold.Isbitwise = bitwise_thrshold_chckBx.Checked; // Bitwise threshold se�ildi�inde bu metodu kullanacak �ekilde parametreyi ayarl�yorum
            UpdatePreviewImage();
        }

        private void thrs_metod_gaussian_rdbx_CheckedChanged(object sender, EventArgs e)
        {
            customParams.Threshold.Method = "gaussian"; // Gaussian method se�ildi�inde bu metodu kullanacak �ekilde parametreyi ayarl�yorum
            UpdatePreviewImage();
        }

        private void thrs_metod_mean_rdbx_CheckedChanged(object sender, EventArgs e)
        {
            customParams.Threshold.Method = "mean"; // Mean method se�ildi�inde bu metodu kullanacak �ekilde parametreyi ayarl�yorum
            UpdatePreviewImage();
        }


        private void otsu_thrshold_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (otsu_thrshold_rdbtn.Checked)
            {
                // ilgili parametre de�i�ti�inde otomatik g�r�nt�ye uygulat�p ekranda g�steriyorum ve alaksa�z olan di�er parametrelerin g�r�n�rl���n� g�ncelliyorum
                thrs_min_value_nud.Enabled = false;
                thrs_adptv_cValue_nud.Enabled = false;
                thrs_adptv_krnlSize_nud.Enabled = false;
                thrs_metod_gaussian_rdbx.Enabled = false; // adaptive threshold se�ildi�inde di�er se�enekleri devre d��� b�rak�yorum
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
                // ilgili parametre de�i�ti�inde otomatik g�r�nt�ye uygulat�p ekranda g�steriyorum
                customParams.Bluring.Type = "gaussen";
                UpdateListBox();
                UpdatePreviewImage();
            }
        }

        private void blr_median_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (blr_median_rdbtn.Checked)
            {
                // ilgili parametre de�i�ti�inde otomatik g�r�nt�ye uygulat�p ekranda g�steriyorum
                customParams.Bluring.Type = "median";
                UpdateListBox();
                UpdatePreviewImage();
            }
        }

        private void blr_blur_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (blr_blur_rdbtn.Checked)
            {
                // ilgili parametre de�i�ti�inde otomatik g�r�nt�ye uygulat�p ekranda g�steriyorum
                customParams.Bluring.Type = "blur";
                UpdateListBox();
                UpdatePreviewImage();
            }
        }

        private void blr_filter_shape_nud_ValueChanged(object sender, EventArgs e)
        {
            // ilgili parametre de�i�ti�inde otomatik g�r�nt�ye uygulat�p ekranda g�steriyorum
            customParams.Bluring.FilterShape = blr_filter_shape_nud.Value;
            UpdateListBox();
            UpdatePreviewImage();
        }


        private void nextImg_btn_Click(object sender, EventArgs e)
        {
            // bir sonraki g�r�nt� se�ildi�inde imageindex'i art�r�p ekranda g�ncelleme i�lemi yap�yorum
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
            // bir sonraki g�r�nt� se�ildi�inde imageindex'i azaltarak bir �nceki resmi g�stermesini sa�l�yorum 
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

                // E�er currentImageIndex art�k liste s�n�rlar� d���ndaysa bir geri git
                if (currentImageIndex >= ApprovedImages.Count)
                {
                    currentImageIndex = ApprovedImages.Count - 1;

                }
                // Liste bo�ald�ysa g�r�nt�y� temizle
                if (ApprovedImages.Count == 0)
                {
                    currentImg_picbox.Image?.Dispose();
                    currentImg_picbox.Image = null;
                    currentImageIndex = 0;

                    unreadyImg_picbox.Image?.Dispose();
                    unreadyImg_picbox.Image = null;

                    approvedImg_picbox.Image?.Dispose();
                    approvedImg_picbox.Image = null;

                    onIsleme_combobx.SelectedIndex = -1; // �n i�leme combobox'�n� s�f�rla
                    setGroupBoxesDefault(); // Grup kutular�n� varsay�lan duruma getir

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
            customParams.Contrast.ClipLimit = clip_limit_nud.Value; // Tile grid size de�i�ti�inde parametreyi g�ncelle
            customParams.Contrast.Type = "Clahe"; // Contrast i�lemi i�in ad�n� g�ncelle

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void tile_grid_size_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.Contrast.GridSize = tile_grid_size_nud.Value; // Tile grid size de�i�ti�inde parametreyi g�ncelle
            customParams.Contrast.Type = "Clahe"; // Contrast i�lemi i�in ad�n� g�ncelle

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void edg_laplacian_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            edg_ksize_nud.Enabled = true; // Laplacian i�in kernel boyutunu etkinle�tiriyorum

            edg_cannyMaxThrs_nud.Enabled = false; // Canny' e ait parametreleri devre d��� b�rak�yorum
            edg_cannyMinThrs_nud.Enabled = false; // Canny' e ait parametreleri devre d��� b�rak�yorum


            customParams.Edge.Type = "laplacian"; // Laplacian kenar alg�lama t�r�n� ayarl�yorum


            UpdateListBox();
            UpdatePreviewImage();

        }

        private void edg_sobelX_rdbtn_CheckedChanged(object sender, EventArgs e)
        {

            edg_ksize_nud.Enabled = true; // sobelX i�in kernel boyutunu etkinle�tiriyorum

            edg_cannyMaxThrs_nud.Enabled = false; // Canny' e ait parametreleri devre d��� b�rak�yorum
            edg_cannyMinThrs_nud.Enabled = false; // Canny' e ait parametreleri devre d��� b�rak�yorum


            customParams.Edge.Type = "sobelX"; // sobelX kenar alg�lama t�r�n� ayarl�yorum

            UpdateListBox();
            UpdatePreviewImage();

        }

        private void edg_sobelY_rdbtn_CheckedChanged(object sender, EventArgs e)
        {

            edg_ksize_nud.Enabled = true; // sobelY i�in kernel boyutunu etkinle�tiriyorum

            edg_cannyMaxThrs_nud.Enabled = false; // Canny' e ait parametreleri devre d��� b�rak�yorum
            edg_cannyMinThrs_nud.Enabled = false; // Canny' e ait parametreleri devre d��� b�rak�yorum


            customParams.Edge.Type = "sobelY"; // sobelY kenar alg�lama t�r�n� ayarl�yorum

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void edg_sobel_rdbtn_CheckedChanged(object sender, EventArgs e)
        {

            edg_ksize_nud.Enabled = true; // sobel i�in kernel boyutunu etkinle�tiriyorum

            edg_cannyMaxThrs_nud.Enabled = false; // Canny' e ait parametreleri devre d��� b�rak�yorum
            edg_cannyMinThrs_nud.Enabled = false; // Canny' e ait parametreleri devre d��� b�rak�yorum


            customParams.Edge.Type = "sobel"; // sobel kenar alg�lama t�r�n� ayarl�yorum

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void edg_canny_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            edg_ksize_nud.Enabled = false; // Canny i�in kernel boyutunu devre d��� b�rak�yorum
            edg_cannyMaxThrs_nud.Enabled = true; // Canny i�in maksimum e�ik de�erini etkinle�tiriyorum
            edg_cannyMinThrs_nud.Enabled = true; // Canny i�in minimum e�ik de�erini etkinle�tiriyorum

            customParams.Edge.Type = "canny"; // Canny kenar alg�lama t�r�n� ayarl�yorum

            UpdateListBox();
            UpdatePreviewImage();

        }



        private void edg_ksize_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.Edge.KernelShape = edg_ksize_nud.Value; // Kernel boyutunu ayarl�yorum

            UpdateListBox();
            UpdatePreviewImage();
        }

        private void edg_cannyMinThrs_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.Edge.MinThrashold = edg_cannyMinThrs_nud.Value; // Canny minimum e�ik de�erini ayarl�yorum

            UpdateListBox();
            UpdatePreviewImage();

        }

        private void edg_cannyMaxThrs_nud_ValueChanged(object sender, EventArgs e)
        {

            customParams.Edge.MaxThrashold = edg_cannyMaxThrs_nud.Value; // Canny maksimum e�ik de�erini ayarl�yorum

            UpdateListBox();
            UpdatePreviewImage();

        }

        private void stdHough_linep_rdbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (stdHough_linep_rdbtn.Checked)
            {
                stdHoughP_grpbx.Enabled = true;
                stdHoughC_grpbx.Enabled = false;

                customParams.HoughTransform.Type = "houghP"; // Hough Transform t�r�n� ayarl�yorum

                UpdateListBox();
                UpdatePreviewImage();
            }




        }


        private void stdHoughL_thrs_nud_ValueChanged(object sender, EventArgs e)
        {
            customParams.HoughTransform.Thrashold = stdHoughL_thrs_nud.Value; // Hough Transform i�in e�ik de�erini ayarl�yorum


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
                // Hough Circle se�ildi�inde ilgili grup kutusunu etkinle�tiriyorum ve di�erini devre d��� b�rak�yorum
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