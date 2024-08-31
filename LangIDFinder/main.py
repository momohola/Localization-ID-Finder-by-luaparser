import hashlib
import os
import json
import sys

from luaparser import ast
from luaparser import astnodes

#lang函数名字
languageFuncName = "lang.Get"

#工程的lua代码路径
projectLuaCodePath = "F:\\vHero\\china_dev\\Bin\\Client\\Assets\\Lua"

#cache文件的保存路径
cacheFilePath = "cacheFile"

#cache文件的文件名
cacheFileName = "luaFinderIndex.json"

#忽略列表
ignoreList = []

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

# 查找函数参数的值
def FindFuncNudeArgs(node, allVarList):
    argsList = []
    if len(node.args) > 0 :
        firstArgs = node.args[0]
        if isinstance(firstArgs, astnodes.Number):
            argsList.append(firstArgs.n)
        elif isinstance(firstArgs, astnodes.Index):    #如果参数是一个table
            if isinstance(firstArgs.value, astnodes.Name) and isinstance(firstArgs.idx, astnodes.Name):         #哈希
                varName = firstArgs.value.id + "." + firstArgs.idx.id
                if varName in allVarList :
                    argsList.append(allVarList[varName]['value'])
            elif isinstance(firstArgs.value, astnodes.Name) and isinstance(firstArgs.idx, astnodes.Number):         #数组
                varName = firstArgs.value.id + "." + str(firstArgs.idx.n)
                if varName in allVarList:
                    argsList.append(allVarList[varName]['value'])
        elif isinstance(firstArgs, astnodes.Name):      #如果参数是一个变量
            if firstArgs.id in allVarList:
                argsList.append(allVarList[firstArgs.id]['value'])
        elif isinstance(firstArgs, astnodes.AddOp):    #如果参数是加法
            if isinstance(firstArgs.right, astnodes.Number) and isinstance(firstArgs.left, astnodes.Number):       #两个数字相加
                argsList.append(firstArgs.right.n + firstArgs.left.n)
            elif isinstance(firstArgs.right, astnodes.Name) or isinstance(firstArgs.left, astnodes.Name):         #数字 + 变量   ro   变量 + 变量
                right = 0
                left = 0
                #查找右值
                if isinstance(firstArgs.right, astnodes.Name) :
                    if firstArgs.right.id in allVarList:
                        right = allVarList[firstArgs.right.id]['value']
                elif isinstance(firstArgs.right, astnodes.Number) :
                    right = firstArgs.right.n

                # 查找左值
                if isinstance(firstArgs.left, astnodes.Name) :
                    if firstArgs.left.id in allVarList:
                        left = allVarList[firstArgs.left.id]['value']
                elif isinstance(firstArgs.left, astnodes.Number):
                    left = firstArgs.left.n

                if right + left > 0:
                    argsList.append(right + left)

    return argsList

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

#返回文件夹下的所有文件
def SeekFile(dir, fileName):
    for path, dirList, fileList in os.walk(dir):
        lowerFile = []
        for file in fileList :
            lowerFile.append(file.lower())

        if fileName in lowerFile:
            return os.path.join(path, fileName)
    return False

#在一个lua文件中搜索langid
def SearchLangIDByStr(str):
    '''
    :param file: 文件
    :return: 搜索到的 call node
    '''
    tree = ast.parse(str)
    invokeLangGetList = FindFuncNude(tree, languageFuncName)
    allVarList = FindAllVariable(tree)
    resNodeList = []
    for node in invokeLangGetList :
        funcArgsLangIDList = FindFuncNudeArgs(node, allVarList)
        if funcArgsLangIDList :
            print("函数行数:",node.line, "函数参数列表:", funcArgsLangIDList)
            temp = {}
            temp["langFuncLine"] = node.line
            temp["langFuncArgs"] = funcArgsLangIDList
            resNodeList.append(temp)
    print("===================================================")
    return resNodeList

#读取缓存文件,返回对象字典
def ReadCacheFile(path):
    '''
    :param path: 缓存文件路径
    :return: 对象列表
    '''
    if os.path.exists(path):
        with open(path, 'r', encoding='utf-8', errors='ignore') as file:
            return json.loads(file.read())
    else:
        return ""

#构建lua索引文件
def BuildIndexFile(cacheContent):
    count = 0
    cacheList = {}  # 文件缓存列表
    for path, dirList, fileList in os.walk(projectLuaCodePath):
        splitPath = path.replace(projectLuaCodePath, "").split("\\")
        if len(splitPath) > 1:
            dirName = splitPath[1]
        else:
            dirName = ""

        # 搜索忽略列表
        isIgnore = False
        for item in ignoreList:
            if item.lower() == dirName.lower():
                isIgnore = True

        # 如果不在忽略列表中
        if not isIgnore:
            for fileName in fileList:
                if fileName.endswith(".txt"):
                    luaFilePath = os.path.join(path, fileName)
                    print(luaFilePath)
                    count = count + 1

                    with open(luaFilePath, 'r', encoding='utf-8', errors='ignore') as luaFile:
                        # luaFile lua代码文件
                        luaStr = luaFile.read()
                        curLuaFileMd5 = GetStringMd5(luaStr)

                        if fileName in cacheContent and curLuaFileMd5 == cacheContent[fileName]["md5"] and languageFuncName == cacheContent[fileName]["languageFuncName"]:
                            #如果md5码相同则直接使用上一次计算的结果
                            cacheList[fileName] = {}
                            cacheList[fileName]["langFuncList"] = cacheContent[fileName]["langFuncList"]
                            cacheList[fileName]["md5"] = curLuaFileMd5
                            cacheList[fileName]["languageFuncName"] = languageFuncName
                        else:
                             # 如果md5码不同则需要重新计算
                            temp = SearchLangIDByStr(luaStr)
                            cacheList[fileName] = {}
                            cacheList[fileName]["langFuncList"] = temp
                            cacheList[fileName]["md5"] = curLuaFileMd5
                            cacheList[fileName]["languageFuncName"] = languageFuncName

    if not os.path.exists(cacheFilePath):
        os.makedirs(cacheFilePath)
    else:
        if os.path.exists(os.path.join(cacheFilePath, cacheFileName)):
            os.remove(os.path.join(cacheFilePath, cacheFileName))

    with open(os.path.join(cacheFilePath, cacheFileName), 'w') as f:
        json.dump(cacheList, f)

    return count

def Start():
    #先读取缓存文件，用于对于md5码
    cacheContent = ReadCacheFile(os.path.join(cacheFilePath, cacheFileName))
    # for key, value in cacheContent.items() :
    #     print(key)
    #     print(value)

    count = BuildIndexFile(cacheContent)
    print("总共构建了%d个文件的索引" % count)

def GetStringMd5(input_string):
    """计算字符串的 MD5 值"""
    # 创建一个 MD5 哈希对象
    md5_hash = hashlib.md5()

    # 更新哈希对象，注意要将字符串编码为字节
    md5_hash.update(input_string.encode('utf-8'))
    return md5_hash.hexdigest()  # 返回十六进制字符串形式的 MD5 值

# 运行参数：argv[1] 项目的路径
# 运行参数：argv[2] 索引文件的名字(不含扩展名)
def GetCommandParam():
    global projectLuaCodePath
    global cacheFileName
    global cacheFilePath
    global languageFuncName

    if len(sys.argv) >= 2:
        projectLuaCodePath = sys.argv[1]
    if len(sys.argv) >= 3 :
        cacheFileName = os.path.basename(sys.argv[2])
        cacheFilePath = os.path.dirname(sys.argv[2])
    if len(sys.argv) >= 4 :
        languageFuncName = sys.argv[3]

if __name__ == '__main__':
    GetCommandParam()

    print("projectLuaCodePath:" + projectLuaCodePath)
    print("cacheFileName:" + cacheFileName)
    print("cacheFilePath:" + cacheFilePath)
    Start()
    print("构建Lua索引文件完成")
    print("LuaFinderExit")
    input("输入任意字符结束")
