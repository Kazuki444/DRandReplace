using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GoogleARCore;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.PhotoModule;
using System;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using System.Linq;

public class DRBackground : MonoBehaviour
{
    public string label;

    public Image DRBackgroundImage;

    private Texture2D m_DRBackgourndTexture = null;

    private DisplayUvCoords m_CameraImageToDisplayUvTransformation;

    //YOLOに必要なファイルと閾値とパラメータ
    public List<string> classesList;
    public float confThreshold = 0.24f;
    public float nmsThreshold = 0.24f;

    private List<string> classNames;
    private List<string> outBlobNames;

    private string model_filepath;
    private string config_filepath;
    private string class_filepath;


    private Net net = null;
    private Size inpSize;        //ネットワークの入力画像のサイズ(高速化：320、通常：416、正確：608)
    private Scalar mean;
    private float scalefactor;

    //Inpaint
    private bool isNeedInpaint = false;

    private void Awake()
    {
        //縦向きに固定
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.orientation = ScreenOrientation.Portrait;

    }


    // Start is called before the first frame update
    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        //YOLO関連の準備
        PrepareYOLO();


    }

    // Update is called once per frame
    void Update()
    {
        ///CPU画像を取得して画像処理を行った後、背景として設定
        using (var image = Frame.CameraImage.AcquireCameraImageBytes())
        {
            if (!image.IsAvailable)
            {
                return;
            }

            if (m_DRBackgourndTexture == null)
            {
                m_DRBackgourndTexture = new Texture2D(image.Width, image.Height, TextureFormat.RGBA32, false, false);
                m_CameraImageToDisplayUvTransformation = Frame.CameraImage.ImageDisplayUvs;
            }


            //色変換
            Mat rgbMat = new Mat(image.Height, image.Width, CvType.CV_8UC3);
            YUVtoRGB(image, rgbMat);

            //転置
            Mat transposeMat = new Mat(image.Width, image.Height, CvType.CV_8UC3);
            transposeMat = rgbMat.t();
            //rgbMat.Dispose();

            //画像処理
            Diminish(transposeMat, net);


            //転置
            Mat DRMat = new Mat(image.Height, image.Width, CvType.CV_8UC3);
            DRMat = transposeMat.t();
            //transposeMat.Dispose();

            //マテリアルのテクスチャに割り当て
            //fastMatToTexture2Dを使ってもいいかも
            Utils.matToTexture2D(DRMat, m_DRBackgourndTexture, false);
            m_DRBackgourndTexture.Apply();
            DRBackgroundImage.material.SetTexture("_ImageTex", m_DRBackgourndTexture);

            rgbMat.Dispose();
            transposeMat.Dispose();
            DRMat.Dispose();

            //スマホのサイズに調整する　←　後で調整
            const string TOP_LEFT_RIGHT = "_UvTopLeftRight";
            const string BOTTOM_LEFT_RIGHT = "_UvBottomLeftRight";

            DRBackgroundImage.material.SetVector(TOP_LEFT_RIGHT, new Vector4(
                m_CameraImageToDisplayUvTransformation.TopLeft.x,
                m_CameraImageToDisplayUvTransformation.TopLeft.y,
                m_CameraImageToDisplayUvTransformation.TopRight.x,
                m_CameraImageToDisplayUvTransformation.TopRight.y));
            DRBackgroundImage.material.SetVector(BOTTOM_LEFT_RIGHT, new Vector4(
                m_CameraImageToDisplayUvTransformation.BottomLeft.x,
                m_CameraImageToDisplayUvTransformation.BottomLeft.y,
                m_CameraImageToDisplayUvTransformation.BottomRight.x,
                m_CameraImageToDisplayUvTransformation.BottomRight.y));



        }

        isNeedInpaint = false;
    }


    //YOLO関連の初期化
    private void PrepareYOLO()
    {
        //必要ファイルの読み込み
        model_filepath = Utils.getFilePath("dnn/yolov3-tiny.weights");
        config_filepath = Utils.getFilePath("dnn/yolov3-tiny.cfg");
        class_filepath = Utils.getFilePath("dnn/coco.names");

        classNames = readClassNames(class_filepath);

        //ネットワークの初期化
        net = Dnn.readNetFromDarknet(config_filepath, model_filepath);
        outBlobNames = GetOutputsNames(net);

        //入力サイズ関連の初期化
        inpSize = new Size(320, 320);
        mean = new Scalar(0.0, 0.0, 0.0);
        scalefactor = 1.0f / 255.0f;
    }


    //出力名を取得
    private List<string> GetOutputsNames(Net net)
    {
        List<string> names = new List<string>();


        MatOfInt outLayers = net.getUnconnectedOutLayers();
        for (int i = 0; i < outLayers.total(); ++i)
        {
            names.Add(net.getLayer(new DictValue((int)outLayers.get(i, 0)[0])).get_name());
        }
        outLayers.Dispose();

        return names;
    }


    /// <summary>
    /// YUV_420_888をBGRに変換
    /// Pixcel3aはNV21
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    private Mat YUVtoRGB(CameraImageBytes image, Mat output)
    {
        int imageSize = image.Width * image.Height;

        //Y,U,Vそれぞれのポインタから1次元の配列を作る
        byte[] YUVbyte = new byte[(int)(imageSize * 1.5f)];

        unsafe
        {
            for (int i = 0; i < imageSize; i++)
            {
                YUVbyte[i] = *((byte*)image.Y.ToPointer() + (i * sizeof(byte)));
            }
            for (int i = 0; i < imageSize / 4; i++)
            {
                YUVbyte[(imageSize) + 2 * i] = *((byte*)image.U.ToPointer() + (i * image.UVPixelStride * sizeof(byte)));
                YUVbyte[(imageSize) + 2 * i + 1] = *((byte*)image.V.ToPointer() + (i * image.UVPixelStride * sizeof(byte)));
            }
        }


        //OpenCVforUnityのｃ++で使えるように、ガベージコレクションを防止(ポインタを固定する)
        GCHandle pinnedArray = GCHandle.Alloc(YUVbyte, GCHandleType.Pinned);
        IntPtr pointer = pinnedArray.AddrOfPinnedObject();

        //YUVをRGBに変換する
        Mat yuvMat = new Mat((int)(image.Height * 1.5f), image.Width, CvType.CV_8UC1);
        Utils.copyToMat(pointer, yuvMat);
        Imgproc.cvtColor(yuvMat, output, Imgproc.COLOR_YUV2RGB_NV21);

        //メモリの開放
        pinnedArray.Free();
        yuvMat.Dispose();

        return output;
    }


    //消去
    private void Diminish(Mat rgbMat, Net net)
    {

        //4D Blobを作成（ネットワーク用）
        Mat blob = Dnn.blobFromImage(rgbMat, scalefactor, inpSize, mean, false, false);

        //モデルを実行
        net.setInput(blob);
        List<Mat> outs = new List<Mat>();
        net.forward(outs, outBlobNames);

        //YOLOの後処理
        Postprocess(rgbMat, outs, net);

        //メモリの開放
        for (int i = 0; i < outs.Count; i++)
        {
            outs[i].Dispose();
        }
        blob.Dispose();


    }

    //YOLOの後処理
    private void Postprocess(Mat frame, List<Mat> outs, Net net)
    {
        List<int> classIdsList = new List<int>();
        List<float> confidencesList = new List<float>();
        List<OpenCVForUnity.CoreModule.Rect> boxesList = new List<OpenCVForUnity.CoreModule.Rect>();

        for (int i = 0; i < outs.Count; ++i)
        {
            float[] positionData = new float[5];
            float[] confidenceData = new float[outs[i].cols() - 5];

            for (int p = 0; p < outs[i].rows(); p++)
            {
                outs[i].get(p, 0, positionData);

                outs[i].get(p, 5, confidenceData);

                int maxIdx = confidenceData.Select((val, idx) => new { V = val, I = idx }).Aggregate((max, working) => (max.V > working.V) ? max : working).I;
                float confidence = confidenceData[maxIdx];

                if (confidence > confThreshold)
                {

                    int centerX = (int)(positionData[0] * frame.cols());
                    int centerY = (int)(positionData[1] * frame.rows());
                    int width = (int)(positionData[2] * frame.cols());
                    int height = (int)(positionData[3] * frame.rows());
                    int left = centerX - width / 2;
                    int top = centerY - height / 2;

                    classIdsList.Add(maxIdx);
                    confidencesList.Add((float)confidence);
                    boxesList.Add(new OpenCVForUnity.CoreModule.Rect(left, top, width, height));
                }
            }
        }

        MatOfRect boxes = new MatOfRect();
        boxes.fromList(boxesList);

        MatOfFloat confidences = new MatOfFloat();
        confidences.fromList(confidencesList);


        MatOfInt indices = new MatOfInt();
        Dnn.NMSBoxes(boxes, confidences, confThreshold, nmsThreshold, indices);

        Mat mask = new Mat(frame.rows(), frame.cols(), CvType.CV_8UC1, Scalar.all(0));
        for (int i = 0; i < indices.total(); ++i)
        {
            int idx = (int)indices.get(i, 0)[0];
            OpenCVForUnity.CoreModule.Rect box = boxesList[idx];
            MakeMask(box.x, box.y, box.x + box.width, box.y + box.height, mask);
            isNeedInpaint = true;

            //1個しか認識しない制限で今は作ってる
            //labelをリストにすれば複数個も可能なはず
            label = classNames[classIdsList[idx]];

        }

        indices.Dispose();
        boxes.Dispose();
        confidences.Dispose();

        if (isNeedInpaint)
        {
            Photo.inpaint(frame, mask, frame, 2, Photo.INPAINT_NS);
        }
        mask.Dispose();
    }


    private void MakeMask(int left, int top, int right, int bottom, Mat mask)
    {
        Imgproc.rectangle(mask, new Point(left, top), new Point(right, bottom), new Scalar(255), -1);
    }

    //coco.namesを読み込んでリストにする
    private List<string> readClassNames(string filename)
    {
        List<string> classNames = new List<string>();

        System.IO.StreamReader cReader = null;
        try
        {
            cReader = new System.IO.StreamReader(filename, System.Text.Encoding.Default);

            while (cReader.Peek() >= 0)
            {
                string name = cReader.ReadLine();
                classNames.Add(name);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex.Message);
            return null;
        }
        finally
        {
            if (cReader != null)
                cReader.Close();
        }

        return classNames;
    }
}