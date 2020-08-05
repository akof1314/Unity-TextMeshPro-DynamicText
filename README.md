# Unity-TextMeshPro-DynamicManager
TextMeshPro DynamicManager 动态文本管理

## 原理

基于`TextMeshPro`1.5.x 版本，注册需要检查的字体图集，在切场景的时候调用检查动态图集纹理数量，大于一定数量时清除重置，防止纹理数越来越多。

## 使用场景

适用于静态字体 + 动态字体结合使用

![](https://github.com/akof1314/Unity-TextMeshPro-DynamicText/raw/master/Pic/2019-11-30_142340.png)

## 静态方法

- TMP_DynamicManager.RegisterFontAssetDynamic(text)
- TMP_DynamicManager.CheckFontAssetDynamic();



## 源码地址

GitHub：[https://github.com/akof1314/Unity-TextMeshPro-DynamicText](https://github.com/akof1314/Unity-TextMeshPro-DynamicText)