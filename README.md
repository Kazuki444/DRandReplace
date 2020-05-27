# DRandReplace
In this study, we developed a system that combines Generic object recognition and image processing with AR technology 
to visually remove real objects and display only virtual objects in the same category as the real objects. 
In this system, categories and regions to be removed get from generic object recognition.
An image which the object erased from by image processing use as a background of AR, and we set a virtual object 
which is the same category as the recognized category. This system operates on one smartphone.

# DEMO
<img src="https://user-images.githubusercontent.com/49668858/82035368-62a15a80-96da-11ea-81a4-c59c31791927.png" width=40%> <img src="https://user-images.githubusercontent.com/49668858/82035364-61702d80-96da-11ea-88f5-465f9f8a157b.png" width=40%>

左：従来のARアプリ　　　右：本システム

[実行動画](https://drive.google.com/open?id=1SDFrPQzhKp-OXAZQZ993pWjoSaDTMDMI)

椅子を認識して、椅子の仮想オブジェクトで置換している
ときどき、実物体である椅子を完全に消去できないときが発生している

事前に数種類の仮想オブジェクトを用意しておく必要がある

# How it works
ARCore-Unity-SDKのHelloARサンプルをもとに作成
ARCore-Unity-SDKのComputerVisionサンプルの要領で画像を取得する

1. YOLOv3-tinyで除去対象のクラスと除去対象領域を認識
1. 除去対象領域をInpainting
1. 除去対象のクラスと同じクラスの仮想オブジェクトで置換

# Note
ARCore-Unity-SDKのComputerVisionのように処理した画像をそのままARBackgroundマテリアルに設定しようとしたが、
上手く動作しなかったため、Canvasに張り付けた

AndroidStudioでTensorflowLiteを使って、FPSあげた[改良バージョン](https://github.com/Kazuki444/ReplaceObjects)できました
