# DRandReplace
In this study, we developed a system that combines Generic object recognition and image processing with AR technology 
to visually remove real objects and display only virtual objects in the same category as the real objects. 
In this system, categories and regions to be removed get from generic object recognition.
An image which the object erased from by image processing use as a background of AR, and we set a virtual object 
which is the same category as the recognized category. This system operates on one smartphone.

# DEMO
![result2](https://user-images.githubusercontent.com/49668858/79317777-129b7080-7f41-11ea-843e-b54dbf0959e1.gif)

椅子を認識して、椅子の仮想オブジェクトで置換している
ときどき、実物体である椅子を完全に消去できないときが発生している

同じ空間に数種類の物体があっても、事前に仮想オブジェクトを用意しておけば対応可能

# How it works
ARCore-Unity-SDKのHelloARサンプルをもとに作成
ARCore-Unity-SDKのComputerVisionサンプルの要領で画像を取得し、YOLOv3-tinyで画像認識を行う
認識結果に対応した仮想オブジェクトを配置する

# Note
ARCore-Unity-SDKのComputerVisionのように処理した画像をそのままARBackgroundマテリアルに設定しようとしたが、
上手く動作しなかったため、Canvasに張り付けた
