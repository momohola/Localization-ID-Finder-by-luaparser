# Localization-ID-Finder-by-luaparser
Unity本地化id查找器，luaparser函数参数查找

> 前言：
> 适用范围：Unity 中需要查找所有预制体里面的某一个脚本的属性值，或者Lua脚本里面的某一个属性值
> 本文介绍如何查找预制体和Lua脚本里面调用的本地化id

下面首先介绍改插件的功能以及使用方法，然后对该插件的原理进行说明

## 使用说明
本插件能够查找到预制体和lua脚本里面那些地方调用了指定的本地化id。操作步骤：首先在LangID输入框中输入要查找的id（使用分号间隔），也可以直接从csv文件导入id。点击【查找】按钮，就可以找到对应的id被哪些预制体和lua代码调用了。

![在这里插入图片描述](https://i-blog.csdnimg.cn/direct/46491c1db11f4632a34365294ed82238.png#pic_center)

【打开】按钮会调用默认编辑器打开对应的文件
【更新Lua代码索引】按钮会运行python程序生成lua代码的索引文件
【设置】按钮用于设置本地化函数名

 ---
 
## 插件原理说明

> 首先需要确保工程已经安装了Odin Inspector插件，否则无法运行本插件。

因为是要查找指定的本地化id，那么可以按照功能模块将代码分为两部分——查找预制体和查找Lua代码。

#### 查找Lua代码中的本地化id
假设获取本地化id的函数名为lang.Get。
那么，是不是使用正则匹配lang.Get这个字符串，然后就可以获取调用的id了呢？实际项目中情况可能更复杂。

通过总结实际项目中代码的编写格式，可以将函数调用分为一下几种情况
```lua
function test()
    print("function test")
    
    --参数为常量
    lang.Get(10000)
    lang.Get(10000 + 10)
    
    --参数包含变量的情况
    local id = 20
    lang.Get(id)
    lang.Get(10000 + id)
    lang.Get(id + 10000)
    
    --参数包含table的情况
    local LANG_ENUM = {
        id1 = 10000,
        id2 = 10001,
        id3 = 10002,
    }
    lang.Get(LANG_ENUM.id1)
    lang.Get(LANG_ENUM.id2)
    lang.Get(LANG_ENUM.id3)
    
    local LANG_ARRAY = {20000, 20001, 20002}
    lang.Get(LANG_ARRAY[1])
    lang.Get(LANG_ARRAY[2])
end

```
面对这些情况，正则就不够用了。为了实现对以上情况的查找，所以lua代码部分的查找工作使用语法树来完成，这里使用基于python的luaparser（也有基于JS的）。luaparser的官方文档：[https://pypi.org/project/luaparser/](https://pypi.org/project/luaparser/)

这里使用python是为了方便将python程序打包，因为大部分人的电脑中没有python环境，而且我们不可能在插件中内嵌一个python环境，所有最后需要将python代码打包成exe程序。

![请添加图片描述](https://i-blog.csdnimg.cn/direct/ab08540f88b54bdb90e4c4d1669bd658.png)

上面是整套python代码的执行流程，简单来说就是首先找出差异文件，然后生成lua代码的语法树，然后找到所有的本地化id，最后将查找的结果进行序列化并以JSON格式保存在本地。C#层会读取该JSON文件，从而查找对应id。

P.S. 原本没有差异文件对比的，后来实际运行发现速度太慢了，构建2000个lua文件的索引大概需要8分钟左右，完全接受不了。后面加入差异化文件对比之后每次构建时间缩短到了1分钟以内，速度大大提高。

下面对python代码进行简单介绍

> ast.walk(tree)：获取树结构进行遍历 
> isinstance(node, astnodes.Call) ：判断该节点是否是函数调用（.调用lua的函数）
> isinstance(node, astnodes.Invoke) ：判断该节点是否是函数调用（：调用lua的函数）
> isinstance(node.func, astnodes.Name)：判断是否是lua的命名表达式

```python
# 查找调用指定函数的Node
def FindFuncNude(tree, funcName = "lang.Get"):
    funcList = []
    funcInfo = funcName.split('.')
    for node in ast.walk(tree):
        if isinstance(node, astnodes.Call) or isinstance(node, astnodes.Invoke):
            if isinstance(node.func, astnodes.Index):
                if isinstance(node.func.value, astnodes.Name) and funcInfo[0] == node.func.value.id:
                    if isinstance(node.func.idx, astnodes.Name) and funcInfo[1] == node.func.idx.id:
                        funcList.append(node)
                        print("lang.Get line->", node.line)
            elif isinstance(node.func, astnodes.Name):
                if node.func.id == funcName :
                    funcList.append(node)
                    print("lang.Get->", node.line)
    return funcList

#获取table里面所有的键值对
def GetTableKeyValuePairs(tableNode):
    '''
    :param tableNode: table 节点
    :return: key：变量名，value：变量的节点
    '''
    keyValueList = {}
    if isinstance(tableNode, astnodes.Table):
        for pair in tableNode.fields :
            if isinstance(pair.key, astnodes.Name) and isinstance(pair.value, astnodes.Number) :          #解析哈希表
                keyValueList[pair.key.id] = pair.value
            elif isinstance(pair.key, astnodes.Number) and isinstance(pair.value, astnodes.Number) :        #解析数组
                keyValueList[str(pair.key.n)] = pair.value
    return keyValueList

#获取文件里面所有的局部变量
def FindAllVariable(tree):
    '''
    :param tree: ast树
    :return: key：变量名，value：{value：变量值，node: 节点}
    '''
    varList = {}
    for node in ast.walk(tree):
        if isinstance(node, astnodes.LocalAssign):
            if len(node.targets) > 0 and len(node.values) > 0 :
                tempVar = node.targets[0]
                tempValue = node.values[0]
                if isinstance(tempVar, astnodes.Name) and isinstance(tempValue, astnodes.Number):    #等号左边是变量  等号右边是字面量
                    varList[tempVar.id] = {"value" : tempValue.n, "node": tempValue}
                elif isinstance(tempVar, astnodes.Name) and isinstance(tempValue, astnodes.Table):   #等号左边是变量  等号右边是table
                    keyValueList = GetTableKeyValuePairs(tempValue)
                    for key in keyValueList :
                        varList[tempVar.id + "." + key] = {"value" : keyValueList[key].n, "node": keyValueList[key]}
    print("---------------------------------")
    for key in varList :
        print(key,"=", varList[key]["value"])
    print("---------------------------------")
    return varList
```
至此，C#端只需要读取python端生成的json文件就可以获取lua的索引信息了，C#端的代码不做信息介绍。完整代码附在末尾的链接里面。

#### 查找预制体中的本地化id
下面介绍如何查找预制体中的本地化id。首先获取所有预制体的路径，然后依次遍历这些路径，使用AssetDatabase.LoadAssetAtPath实例化预制，然后搜索对应的本地化脚本组件即可。

```csharp
        public static Dictionary<int ,Dictionary<string, List<string>>> Finder(string prefabPath, HashSet<int> langIDSet)
        {
            List<string> allPrefabDir = Util.GetAllPrefabDir(prefabPath);
            Dictionary<int ,Dictionary<string, List<string>>> resDic = new Dictionary<int ,Dictionary<string, List<string>>>();
            foreach (var dir in allPrefabDir)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(dir);
                if (prefab != null)
                {
                    LocalizationText[] localizeTextList = prefab.GetComponentsInChildren<LocalizationText>(true);
                    foreach (var localizeText in localizeTextList)
                    {
                        int compLangID = localizeText.TextKey;
                        // 如果找到了
                        if (langIDSet.Contains(compLangID))
                        {
                            if (!resDic.ContainsKey(compLangID))
                            {
                                resDic[compLangID] = new Dictionary<string, List<string>>();
                            }

                            if (!resDic[compLangID].ContainsKey(dir))
                            {
                                resDic[compLangID][dir] = new List<string>();
                            }
                            
                            resDic[compLangID][dir].Add(Util.GetRoute(localizeText.transform));
                        }
                    }
                }
            }

            return resDic;
        }
```

### 代码自取：
langIDFinder 文件为unity插件的代码，其中的python代码位于main.py中
