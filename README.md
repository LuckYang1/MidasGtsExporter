# MidasGtsExporter
用于将Midas GTS NX软件生成的有限元模型导出为FLAC3D、ABAQUS、LS-DYNA等数值分析软件所需要的网格数据文件的工具。  

## 支持的导出格式  
* FLAC3D  
* ABAQUS  
* LS-DYNA  

## 使用方法  
1. 在Midas GTS NX中完成网格划分和单元分组后，在菜单中点击"导出...->GTS NX中性文件..."，得到一个后缀名为".fpn"的模型数据文件；  
2. 启动MidasGtsExporter软件，指定上一步中所导出的fpn文件的路径，设置必要的选项后，点击"转换"按钮执行转换，即可得到指定导出格式的网格数据文件。

## Revision history
* 2018-4-4: 创建Git项目。  
* 2018-4-5: 
  + 将原有的代码由SVN迁移至Git；  
  + 添加了转换时钟和打开输出文件目录的功能；
  + 改进了GTS FPN文件的解析和转换算法，软件性能得到大幅提升，上百万规模的节点和单元数据，转换时间也只需要数秒。  