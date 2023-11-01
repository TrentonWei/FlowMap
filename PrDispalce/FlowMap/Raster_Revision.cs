using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.Geoprocessing;
using AuxStructureLib;
using AuxStructureLib.IO;

namespace PrDispalce.FlowMap
{
    /// <summary>
    /// 考虑加入高程信息的处理窗体（比如人口密度作为高程值）
    /// </summary>
    public partial class Raster_Revision : Form
    {
        public Raster_Revision(AxMapControl axMapControl)
        {
            InitializeComponent();
            this.pMap = axMapControl.Map;
            this.pMapControl = axMapControl;
        }

        #region 参数
        AxMapControl pMapControl;
        IMap pMap;
        PrDispalce.FlowMap.FeatureHandle pFeatureHandle = new FeatureHandle();
        FlowSup Fs = new FlowSup();
        string OutlocalFilePath, OutfileNameExt, OutFilePath;
        PrDispalce.FlowMap.PublicUtil Pu = new PublicUtil();
        FlowMapUtil FMU = new FlowMapUtil();
        PrDispalce.FlowMap.Symbolization Sb = new Symbolization();//可视化测试
        #endregion

        /// <summary>
        /// 深拷贝（通用拷贝）
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static object Clone(object obj)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(memoryStream, obj);
            memoryStream.Position = 0;
            return formatter.Deserialize(memoryStream);
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RasterProcessing_Load(object sender, EventArgs e)
        {
            if (this.pMap.LayerCount <= 0)
                return;

            #region 添加图层
            ILayer pLayer;
            string strLayerName;
            for (int i = 0; i < this.pMap.LayerCount; i++)
            {
                pLayer = this.pMap.get_Layer(i);
                strLayerName = pLayer.Name;
                IDataset LayerDataset = pLayer as IDataset;

                if (LayerDataset != null)
                {
                    ///添加栅格图层
                    IWorkspaceName ws = ((IDatasetName)(LayerDataset.FullName)).WorkspaceName;
                    if(ws.WorkspaceFactoryProgID.IndexOf("RasterWorkspaceFactory")>-1)//判断是否是影像图层
                    {
                        this.comboBox1.Items.Add(strLayerName);//添加影像图层
                    }

                    ///添加点图层
                    if (ws.WorkspaceFactoryProgID.IndexOf("esriDataSourcesFile.ShapefileWorkspaceFactory")>-1)
                    {
                        IFeatureLayer pFeatureLayer = pLayer as IFeatureLayer;
                        this.comboBox4.Items.Add(strLayerName);

                        if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPoint)
                        {
                            this.comboBox2.Items.Add(strLayerName);//添加点图层
                        }
                        if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                        {
                            this.comboBox5.Items.Add(strLayerName);//添加点图层
                        }
                    }
                }
            }
            #endregion

            #region 默认显示第一个
            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
            }
            if (this.comboBox2.Items.Count > 0)
            {
                this.comboBox2.SelectedIndex = 0;
            }
            if (this.comboBox4.Items.Count > 0)
            {
                this.comboBox4.SelectedIndex = 0;
            }
            if (this.comboBox5.Items.Count > 0)
            {
                this.comboBox5.SelectedIndex = 0;
            }
            #endregion
        }

        /// <summary>
        /// RasterRead
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            IRasterLayer rLayer = pFeatureHandle.GetRasterLayer(pMap, this.comboBox1.Text);//获取图层
            IRasterBandCollection rasBandCol = (IRasterBandCollection)rLayer.Raster;
            IRawBlocks rawBlocks;
            IRasterInfo rasInfo;
            IPixelBlock pb;

            // Iterate through each band of the dataset.
            for (int m = 0; m <= rasBandCol.Count - 1; m++)
            {
                // QI to IRawBlocks from IRasterBandCollection.
                rawBlocks = (IRawBlocks)rasBandCol.Item(m);
                rasInfo = rawBlocks.RasterInfo;
                // Create the pixel block.
                pb = rawBlocks.CreatePixelBlock();

                // Determine the tiling scheme for the raster dataset.

                int bStartX = (int)Math.Floor((rasInfo.Extent.Envelope.XMin -
                    rasInfo.Origin.X) / (rasInfo.BlockWidth * rasInfo.CellSize.X));
                int bEndX = (int)Math.Ceiling((rasInfo.Extent.Envelope.XMax -
                    rasInfo.Origin.X) / (rasInfo.BlockWidth * rasInfo.CellSize.X));
                int bStartY = (int)Math.Floor((rasInfo.Origin.Y -
                    rasInfo.Extent.Envelope.YMax) / (rasInfo.BlockHeight *
                    rasInfo.CellSize.Y));
                int bEndY = (int)Math.Ceiling((rasInfo.Origin.Y -
                    rasInfo.Extent.Envelope.YMin) / (rasInfo.BlockHeight *
                    rasInfo.CellSize.Y));

                // Iterate through the pixel blocks.
                for (int pbYcursor = bStartY; pbYcursor < bEndY; pbYcursor++) //Y方向上的迭代
                {
                    for (int pbXcursor = bStartX; pbXcursor < bEndX; pbXcursor++) //X方向上的迭代
                    {
                        // Get the pixel block.
                        rawBlocks.ReadBlock(pbXcursor, pbYcursor, 0, pb);
                        System.Array safeArray;
                        // Put the pixel block into a SafeArray for manipulation.
                        safeArray = (System.Array)pb.get_SafeArray(0);//能读取所有属性

                        ///RaWBlock的值排列从左往右，从上往下 0到n
                        for (int safeArrayHeight = 0; safeArrayHeight < pb.Height; safeArrayHeight++)
                        {
                            for (int safeArrayWidth = 0; safeArrayWidth < pb.Width; safeArrayWidth++)
                            {
                                safeArray.GetValue(safeArrayWidth,safeArrayHeight);
                            }
                        }

                        int TestLoc = 0;

                    }
                }
            }
        }

        /// <summary>
        /// GridCreate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            #region OutPut Check
            if (OutFilePath == null)
            {
                MessageBox.Show("Please give the OutPut path");
                return;
            }
            #endregion

            #region OD参数
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            FMU.GetOD(pFeatureClass, OriginPoint, DesPoints, AllPoints, PointFlow, pMap.SpatialReference);
            #endregion

            #region 获取Grids和节点编码
            IRasterLayer rLayer = pFeatureHandle.GetRasterLayer(pMap, this.comboBox1.Text);//获取图层
            IRasterBandCollection rasBandCol = (IRasterBandCollection)rLayer.Raster;
            IRawBlocks rawBlocks=(IRawBlocks)rasBandCol.Item(0);
            IRasterInfo rasInfo = rawBlocks.RasterInfo;
            double X = rasInfo.CellSize.X; double Y = rasInfo.CellSize.Y;
            //int rowNum = (int)Math.Ceiling(rasInfo.Height / Y); int colNum = (int)Math.Ceiling(rasInfo.Width / X);
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2]; GridXY[0] = X; GridXY[1] = Y;
            double[] ExtendValue = new double[4];
            ExtendValue[0] = rasInfo.Extent.XMin; ExtendValue[1] = rasInfo.Extent.YMin; ExtendValue[2] = rasInfo.Extent.XMax; ExtendValue[3] = rasInfo.Extent.YMax;  
            ///Grids的排列是从左下角开始，从左往右，从下往上
            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGridNonExtend(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网
            #endregion

            #region 输出网格
            SMap OutMap = new SMap();
            foreach (KeyValuePair<Tuple<int, int>, List<double>> Kv in Grids)
            {
                List<TriNode> NodeList = new List<TriNode>();

                TriNode Node1 = new TriNode();
                Node1.X = Kv.Value[0];
                Node1.Y = Kv.Value[1];

                TriNode Node2 = new TriNode();
                Node2.X = Kv.Value[2];
                Node2.Y = Kv.Value[1];

                TriNode Node3 = new TriNode();
                Node3.X = Kv.Value[2];
                Node3.Y = Kv.Value[3];

                TriNode Node4 = new TriNode();
                Node4.X = Kv.Value[0];
                Node4.Y = Kv.Value[3];

                NodeList.Add(Node1); NodeList.Add(Node2); NodeList.Add(Node3); NodeList.Add(Node4);

                TriNode MidNode = new TriNode();
                MidNode.X = (Kv.Value[0] + Kv.Value[2]) / 2;
                MidNode.Y = (Kv.Value[1] + Kv.Value[3]) / 2;
                PointObject CachePoint = new PointObject(0, MidNode);

                PolygonObject CachePo = new PolygonObject(0, NodeList);
                OutMap.PolygonList.Add(CachePo);
                OutMap.PointList.Add(CachePoint);
            }
            #endregion

            #region 网格赋值
            IPixelBlock pb = rawBlocks.CreatePixelBlock();         
            rawBlocks.ReadBlock(0, 0, 0, pb);// Get the pixel block.
            System.Array safeArray;         
            safeArray = (System.Array)pb.get_SafeArray(0);//// Put the pixel block into a SafeArray for manipulation.能读取所有属性

            ///注意：我们以行列标识；RawBlock以XY标识
            Dictionary<Tuple<int, int>, double> ValueGrids = new Dictionary<Tuple<int, int>, double>();
            for (int i = 0; i < rowNum; i++)
            {
                for (int j = 0; j < colNum; j++)
                {
                    Tuple<int, int> CacheGrid = new Tuple<int, int>(i, j);
                    double Value = Convert.ToDouble(safeArray.GetValue(j, rowNum - i - 1));///注意不同的编码规则
                    ValueGrids.Add(CacheGrid, Value);
                }
            }
            #endregion

            OutMap.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
        }

        /// <summary>
        /// 输出路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fdialog = new FolderBrowserDialog();
            string outfilepath = null;

            if (fdialog.ShowDialog() == DialogResult.OK)
            {
                string Path = fdialog.SelectedPath;
                outfilepath = Path;
            }

            OutFilePath = outfilepath;
            this.comboBox3.Text = OutFilePath;
        }

        /// <summary>
        /// FlowMap Generation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            #region OutPut Check
            if (OutFilePath == null)
            {
                MessageBox.Show("Please give the OutPut path");
                return;
            }
            #endregion

            #region OD参数
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            FMU.GetOD(pFeatureClass, OriginPoint, DesPoints, AllPoints, PointFlow, pMap.SpatialReference);
            #endregion

            #region 获取Grids和节点编码
            IRasterLayer rLayer = pFeatureHandle.GetRasterLayer(pMap, this.comboBox1.Text);//获取图层
            IRasterBandCollection rasBandCol = (IRasterBandCollection)rLayer.Raster;
            IRawBlocks rawBlocks = (IRawBlocks)rasBandCol.Item(0);
            IRasterInfo rasInfo = rawBlocks.RasterInfo;
            double X = rasInfo.CellSize.X; double Y = rasInfo.CellSize.Y;
            //int rowNum = (int)Math.Ceiling(rasInfo.Height / Y); int colNum = (int)Math.Ceiling(rasInfo.Width / X);
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2]; GridXY[0] = X; GridXY[1] = Y;
            double[] ExtendValue = new double[4];
            ExtendValue[0] = rasInfo.Extent.XMin; ExtendValue[1] = rasInfo.Extent.YMin; ExtendValue[2] = rasInfo.Extent.XMax; ExtendValue[3] = rasInfo.Extent.YMax;
            ///Grids的排列是从左下角开始，从左往右，从下往上
            Dictionary<Tuple<int, int>, List<double>> CacheGrids = Fs.GetGridNonExtend(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网

            #region 考虑边界的格网构建
            IFeatureLayer CacheLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
            List<IFeatureLayer> LayerList = new List<IFeatureLayer>();
            LayerList.Add(CacheLayer);
            List<Tuple<IGeometry, esriGeometryType>> Features = FMU.GetFeatures(LayerList, pMap.SpatialReference);
            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetInGrid(CacheGrids, Features, 0.02);
            #endregion

            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）
            #endregion

            #region 输出网格
            SMap OutMap = new SMap();
            object PolygonSymbol = Sb.PolygonSymbolization(0.4, 153, 153, 153, 0, 0, 20, 20);
            foreach (KeyValuePair<Tuple<int, int>, List<double>> Kv in Grids)
            {
                List<TriNode> NodeList = new List<TriNode>();

                TriNode Node1 = new TriNode();
                Node1.X = Kv.Value[0];
                Node1.Y = Kv.Value[1];

                TriNode Node2 = new TriNode();
                Node2.X = Kv.Value[2];
                Node2.Y = Kv.Value[1];

                TriNode Node3 = new TriNode();
                Node3.X = Kv.Value[2];
                Node3.Y = Kv.Value[3];

                TriNode Node4 = new TriNode();
                Node4.X = Kv.Value[0];
                Node4.Y = Kv.Value[3];

                NodeList.Add(Node1); NodeList.Add(Node2); NodeList.Add(Node3); NodeList.Add(Node4);

                TriNode MidNode = new TriNode();
                MidNode.X = (Kv.Value[0] + Kv.Value[2]) / 2;
                MidNode.Y = (Kv.Value[1] + Kv.Value[3]) / 2;
                PointObject CachePoint = new PointObject(0, MidNode);

                PolygonObject CachePo = new PolygonObject(0, NodeList);
                OutMap.PolygonList.Add(CachePo);
                OutMap.PointList.Add(CachePoint);

                IPolygon pPolygon = this.PolygonObjectConvert(CachePo);
                pMapControl.DrawShape(pPolygon, ref PolygonSymbol);
            }
            #endregion

            #region 网格赋值
            IPixelBlock pb = rawBlocks.CreatePixelBlock();
            rawBlocks.ReadBlock(0, 0, 0, pb);// Get the pixel block.
            System.Array safeArray;
            safeArray = (System.Array)pb.get_SafeArray(0);//// Put the pixel block into a SafeArray for manipulation.能读取所有属性

            ///注意：我们以行列标识；RawBlock以XY标识
            Dictionary<Tuple<int, int>, double> ValueGrids = new Dictionary<Tuple<int, int>, double>();
            for (int i = 0; i < rowNum; i++)
            {
                for (int j = 0; j < colNum; j++)
                {
                    Tuple<int, int> CacheGrid = new Tuple<int, int>(i, j);
                    if (Grids.ContainsKey(CacheGrid))
                    {
                        #region 以人口编码
                        double Value = Convert.ToDouble(safeArray.GetValue(j, rowNum - i - 1));///注意不同的编码规则
                        ValueGrids.Add(CacheGrid, Value);
                        #endregion

                        #region 编码都一样
                        //double Value = 10;
                        //ValueGrids.Add(CacheGrid, Value);
                        #endregion
                    }
                }
            }
            #endregion

            #region 初始化
            Tuple<int, int> sGrid = NodeInGrid[OriginPoint];//起点格网编码
            List<Tuple<int, int>> desGrids = new List<Tuple<int, int>>();//终点格网编码
            for (int i = 0; i < DesPoints.Count; i++)
            {
                desGrids.Add(NodeInGrid[DesPoints[i]]);
            }

            cFlowMap cFM = new cFlowMap(sGrid, desGrids, PointFlow);
            Dictionary<Tuple<int, int>, double> pWeighGrids = ValueGrids;//确定整个Grids的权重(这里参数需要设置)[每个格网的权重以人口密度代替]
            Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> DesDirPt = FMU.GetDesDirPt_3(GridWithNode, pWeighGrids, desGrids, Grids, 0);//考虑权重（按权重的大小添加）
            #endregion

            #region 遍历构成Flow过程
            double CountLabel = 0;//进程监控
            while (desGrids.Count > 0)
            {
                CountLabel++; Console.WriteLine(CountLabel);//进程监控
                double MaxDis = 0;
                Path CachePath = null;
                //List<Tuple<int, int>> TestTargetPath = null;//测试用
                //double TestShort = 0;//测试用

                for (int i = 0; i < desGrids.Count; i++)
                {
                    double MinLength = 100000;
                    List<Tuple<int, int>> TargetPath = null;
                    int Label = 0;//标识终点到起点最短距离的节是否是起点（=1表示终点是起点）
                    int AngleLabel = 0;//标识最终选用的路径是否非钝角流入（0钝角流入；1非钝角流入）

                    #region 需要判断该节点是否被已构建的路径覆盖(已覆盖)
                    if (FMU.IntersectGrid(desGrids[i], cFM.PathGrids))//判断该节点是否与已有路径重叠
                    {
                        TargetPath = new List<Tuple<int, int>>();
                        TargetPath.Add(desGrids[i]);
                    }
                    #endregion

                    #region 无覆盖
                    else
                    {
                        //每次更新网格权重
                        Console.Write(i);
                        double MinDis = FMU.GetMinDis(desGrids[i], cFM.PathGrids);
                        //List<double> DisList = DesDis.Values.ToList(); DisList.Sort();//升序排列
                        //double Order = DisList.IndexOf(DesDis[desGrids[i]]) / DisList.Count;

                        #region 获取到PathGrid的路径
                        Dictionary<int, PathTrace> DirPt = DesDirPt[desGrids[i]];
                        for (int j = 0; j < cFM.PathGrids.Count; j++)
                        {
                            #region 获取终点到已有路径中某点的最短路径

                            #region 不考虑方向限制
                            //List<int> DirList = new List<int>();

                            //DirList.Add(1);
                            //DirList.Add(2);
                            //DirList.Add(3);
                            //DirList.Add(4);
                            //DirList.Add(5);
                            //DirList.Add(6);
                            //DirList.Add(7);
                            //DirList.Add(8);
                            #endregion

                            List<int> DirList = FMU.GetConDirR(cFM.PathGrids[j], desGrids[i]);//获取限制性约束的方向                   
                            int DirID = FMU.GetNumber(DirList);

                            #region 可能存在重合点的情况
                            if (DirID == 0)
                            {
                                break;
                            }
                            #endregion

                            List<Tuple<int, int>> CacheShortPath = DirPt[DirID].GetShortestPath(cFM.PathGrids[j], desGrids[i]);
                            #endregion

                            #region 需要考虑可能不存在路径的情况
                            double CacheShortPathLength = 0;
                            if (CacheShortPath != null)
                            {
                                CacheShortPathLength = FMU.GetPathLength(CacheShortPath);
                            }
                            else
                            {
                                CacheShortPathLength = 10000000;
                            }
                            #endregion

                            #region 添加交叉约束
                            if (CacheShortPath != null)
                            {
                                List<Tuple<int, int>> CopydesGrids = Clone((object)desGrids) as List<Tuple<int, int>>;
                                bool OverlayLabel = FMU.IntersectPath(CacheShortPath, CopydesGrids);//判断是否重叠

                                #region 存在交叉
                                if (FMU.IntersectPathInt(CacheShortPath, cFM.PathGrids) == 2)//这里的相交修改了
                                {
                                    CacheShortPathLength = 1000000 + CacheShortPathLength;//交叉惩罚系数更高
                                }
                                #endregion

                                #region 存在重叠
                                else if (FMU.IntersectPathInt(CacheShortPath, cFM.PathGrids) == 1 || OverlayLabel)
                                {
                                    //if (MinDis < Math.Sqrt(1.9) && (CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.45) > MaxDis)///如果长度小于该值才需要判断；否则无须判断
                                    //if (MinDis < Math.Sqrt(1.9))
                                    //if (MinDis < 2.9)
                                    //{
                                    #region 考虑到搜索方向固定可能导致的重合
                                    Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝
                                    FMU.FlowCrosssingContraint(WeighGrids, 0, desGrids[i], cFM.PathGrids[j], cFM.PathGrids);//Cross约束
                                    //FMU.FlowOverLayContraint_2(desGrids, WeighGrids, 1, desGrids[i]);//Overlay约束
                                    //FMU.FlowOverLayContraint_2Tar(desGrids, WeighGrids, 0, desGrids[i]);
                                    FMU.FlowOverLayContraint_3Tar(desGrids, GridWithNode, WeighGrids, 0, desGrids[i], Grids, true);//0就是1阶；0阶时该段代码注释掉即可

                                    PathTrace Pt = new PathTrace();
                                    List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                                    JudgeList.Add(desGrids[i]);//添加搜索的起点
                                    Pt.MazeAlg(JudgeList, WeighGrids, 1, DirList);//备注：每次更新以后,WeightGrid会清零  
                                    CacheShortPath = Pt.GetShortestPath(cFM.PathGrids[j], desGrids[i]);

                                    if (CacheShortPath != null)
                                    {
                                        CacheShortPathLength = FMU.GetPathLength(CacheShortPath);

                                        #region 判段交叉或重合
                                        if (FMU.IntersectPathInt(CacheShortPath, cFM.PathGrids) == 2 || FMU.IntersectPath(CacheShortPath, CopydesGrids))
                                        {
                                            CacheShortPathLength = 1000000 + CacheShortPathLength;//交叉惩罚系数更高
                                        }
                                        #endregion
                                    }
                                    else
                                    {
                                        CacheShortPathLength = 10000000;
                                    }
                                    #endregion
                                    //}

                                    //否则，加上一个极大值
                                    //else
                                    //{
                                    //    CacheShortPathLength = 100000 + CacheShortPathLength;
                                    //}
                                }
                                #endregion

                                //if (FMU.IntersectPath(CacheShortPath, cFM.PathGrids))
                                //{
                                //    CacheShortPathLength = 100000 + CacheShortPathLength;
                                //}

                                //if (FMU.LineIntersectPath(CacheShortPath, cFM.PathGrids, Grids))
                                //{
                                //    CacheShortPathLength = 1000000 + CacheShortPathLength;
                                //}

                                //if (FMU.obstacleIntersectPath(CacheShortPath, Features, Grids))
                                //{
                                //    CacheShortPathLength = 1000000 + CacheShortPathLength;
                                //}

                            }
                            #endregion

                            #region 添加角度约束限制
                            if (CacheShortPath != null)
                            {
                                if (FMU.AngleContraint(CacheShortPath, cFM.GridForPaths[cFM.PathGrids[j]], Grids))
                                {
                                    CacheShortPathLength = 15 + CacheShortPathLength;
                                }
                            }
                            #endregion

                            #region 添加长度约束
                            //if (CacheShortPathLength < Math.Sqrt(1.9))
                            //if (CacheShortPathLength < 2.9)
                            //{
                            //CacheShortPathLength = 15 + CacheShortPathLength;
                            //}
                            #endregion

                            #region 添加分段约束限制
                            List<Path> PathList = cFM.GetDividedPath(cFM.PathGrids[j]);
                            if (PathList != null)
                            {
                                List<double> LengthList = new List<double>();
                                for (int k = 0; k < PathList.Count; k++)
                                {
                                    if (PathList[k] != null)
                                    {
                                        double CacheLength = PathList[k].GetPathLength();
                                        LengthList.Add(CacheLength);
                                    }
                                }

                                for (int k = 0; k < LengthList.Count; k++)
                                {
                                    if (LengthList[k] != 0 && LengthList[k] < 2)
                                    {
                                        CacheShortPathLength = 15 + CacheShortPathLength;
                                    }
                                }
                            }
                            #endregion

                            double TotalLength = 0;
                            TotalLength = CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.65;
                            #region 比较获取某给定节点到起点的最短路径
                            if (TotalLength < MinLength)
                            {
                                if (cFM.GridForPaths[cFM.PathGrids[j]].Length == 0 && !cFM.PathGrids.Contains(CacheShortPath[1]))//消除某些点到起点的最短路径是经过已有路径的情况
                                {
                                    Label = 1;//标识最短路径终点是起点
                                }

                                else
                                {
                                    Label = 0;//标识最短路径终点非起点
                                }

                                MinLength = TotalLength;
                                List<Tuple<int, int>> pCachePath = cFM.GridForPaths[cFM.PathGrids[j]].ePath.ToList();
                                CacheShortPath.RemoveAt(0);//移除第一个要素，避免存在重复元素

                                pCachePath.AddRange(CacheShortPath);
                                TargetPath = pCachePath;

                                //if (FMU.AngleContraint(TargetPath, cFM.GridForPaths[cFM.PathGrids[j]], Grids))
                                //{
                                //    AngleLabel = 1;//非钝角流入
                                //}
                                //else
                                //{
                                //    AngleLabel = 0;//钝角流入
                                //}

                                //TestTargetPath = CacheShortPath;//测试用
                                //TestShort = CacheShortPathLength;//测试用
                            }
                            #endregion
                        }
                        #endregion
                    }
                    //}
                    #endregion

                    #region 表示起点优先限制
                    //if (Label == 1)
                    //{
                    //    MinLength = MinLength + 10000;
                    //}
                    #endregion

                    #region 添加角度约束【特殊情况-一般不需要考虑该情况】
                    //if (AngleLabel==1)
                    //{
                    //    MinLength = MinLength - 10000;
                    //}
                    #endregion

                    #region 获取到起点路径最长终点的路径
                    if (TargetPath == null)//可能存在路径为空的情况
                    {
                        MinLength = 0;
                    }

                    if (MinLength > MaxDis)
                    {
                        MaxDis = MinLength;
                        CachePath = new Path(sGrid, desGrids[i], TargetPath);

                        //double Test = FMU.GetPathLength(TestTargetPath);//测试用
                        //int TesTloc = 0;
                    }
                    #endregion
                }

                #region 可视化显示（表示Path的生成过程）
                FlowDraw FD1 = new FlowDraw(pMapControl, Grids);
                FD1.FlowPathDraw(CachePath, 1, 0);
                #endregion

                #region 防止可能出现空的情况
                //cFM.PathRefresh(CachePath, 1);
                if (CachePath != null)
                {
                    cFM.PathRefresh(CachePath, 1, PointFlow[GridWithNode[CachePath.endPoint]]);//更新：包括子路径与流量更新
                    desGrids.Remove(CachePath.endPoint);//移除一个Destination
                    //DesDis.Remove(CachePath.endPoint);
                }

                else
                {
                    desGrids.RemoveAt(0);
                }
                #endregion
            }
            #endregion

            #region 可视化和输出(TVCG修改前)
            FlowDraw FD2 = new FlowDraw(pMapControl, Grids, AllPoints, NodeInGrid, GridWithNode, OutFilePath);
            //FD2.SmoothFlowMap(cFM.SubPaths, 2, 2000, 20, 1, 1, 1, 0, 200);
            //FD2.FlowMapDraw(cFM.SubPaths, 15, 2000, 20, 1, 1, 1);
            //FD2.FlowMapDraw(cFM.SubPaths, 15, 2000, 20, 1, 0, 1);
            #endregion

            #region 可视化和输出(8.22 TVCG修改)
            //FlowDraw FD2 = new FlowDraw(pMapControl, Grids, AllPoints, NodeInGrid, GridWithNode, OutFilePath);
            //FD2.SmoothFlowMap_2(cFM.SubPaths, 4, 0.05, 2000, MinFlow, 2, 1, 0.4, 0.1 * GridXY[0], 1, 200, 0, 0.001, true, true, 0);//三类边控制点的偏移距离为格网长度的0.1
            FD2.FlowMapDraw(cFM.SubPaths, 2, 0.05, 2000, 20, 1, 1, 1);//Grid Connect Layout
            FD2.FlowMapDraw(cFM.SubPaths, 2, 0.05, 2000, 20, 1, 0, 1);//Point Connect Layout
            #endregion
        }
        
        /// <summary>
        /// 提高效率的策略1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            #region OutPutCheck
            if (OutFilePath == null)
            {
                MessageBox.Show("Please give the OutPut path");
                return;
            }
            #endregion

            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();
            #endregion

            #region AlphaShape
            DelaunayTin dt = new DelaunayTin(map.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
            List<TriEdge> EdgeList = dt.CreateAlphaShape2(8);

            //dt.TriEdgeList = EdgeList;
            //dt.WriteShp(OutFilePath, pMap.SpatialReference);

            List<PolygonObject> PoList = dt.GetAlphaShape(EdgeList);
            //map.PolygonList = PoList;
            //map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            #endregion

            #region OD参数
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;

            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            FMU.GetOD(pFeatureClass, OriginPoint, DesPoints, AllPoints, PointFlow, pMap.SpatialReference);
            List<double> AllFlows = PointFlow.Values.ToList();
            AllFlows.Remove(0);//起点无流量，需要删除
            double MinFlow = AllFlows.Min(); double MaxFlow = AllFlows.Max();//最大流量和最小流量
            double SumFlow = AllFlows.Sum();//总流量
            #endregion

            #region 获取Grids和节点编码
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            //GridXY[0] = 0.8; GridXY[1] = 0.8;
            GridXY = Fs.GetXY(AllPoints, 2, 0.05);
            double[] ExtendValue = FMU.GetExtend(pFeatureLayer);

            #region 正常格网构建
            Dictionary<Tuple<int, int>, List<double>> CacheGrids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网  
            #endregion

            #region 考虑阻隔构建格网
            List<Tuple<IGeometry, esriGeometryType>> Features = new List<Tuple<IGeometry, esriGeometryType>>();
            for(int i=0;i<PoList.Count;i++)
            {
                Tuple<IGeometry, esriGeometryType> CacheTuple = new Tuple<IGeometry, esriGeometryType>(this.PolygonObjectConvert(PoList[i]) as IGeometry, esriGeometryType.esriGeometryPolygon);
                Features.Add(CacheTuple);
            }
            Dictionary<Tuple<int, int>, List<double>> Grids=Fs.GetInGrid(CacheGrids,Features,0); 
            //IFeatureLayer CacheLayer = pFeatureHandle.GetLayer(pMap, this.comboBox3.Text);//阻隔图层
            //List<IFeatureLayer> LayerList = new List<IFeatureLayer>();
            //LayerList.Add(CacheLayer);
            //List<Tuple<IGeometry, esriGeometryType>> Features = FMU.GetFeatures(LayerList, pMap.SpatialReference);
            //Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGridConObstacle(ExtendValue, GridXY, Features, ref colNum, ref rowNum, 0);//构建格网
            #endregion

            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）
            #endregion

            #region 网格绘制（可有可无）
            object PolygonSymbol = Sb.PolygonSymbolization(0.4, 153, 153, 153, 0, 0, 20, 20);
            foreach (KeyValuePair<Tuple<int, int>, List<double>> Kv in Grids)
            {
                List<TriNode> NodeList = new List<TriNode>();

                TriNode Node1 = new TriNode();
                Node1.X = Kv.Value[0];
                Node1.Y = Kv.Value[1];

                TriNode Node2 = new TriNode();
                Node2.X = Kv.Value[2];
                Node2.Y = Kv.Value[1];

                TriNode Node3 = new TriNode();
                Node3.X = Kv.Value[2];
                Node3.Y = Kv.Value[3];

                TriNode Node4 = new TriNode();
                Node4.X = Kv.Value[0];
                Node4.Y = Kv.Value[3];

                NodeList.Add(Node1); NodeList.Add(Node2); NodeList.Add(Node3); NodeList.Add(Node4);

                TriNode MidNode = new TriNode();
                MidNode.X = (Kv.Value[0] + Kv.Value[2]) / 2;
                MidNode.Y = (Kv.Value[1] + Kv.Value[3]) / 2;
                PointObject CachePoint = new PointObject(0, MidNode);

                PolygonObject CachePo = new PolygonObject(0, NodeList);
                IPolygon pPolygon = this.PolygonObjectConvert(CachePo);
                pMapControl.DrawShape(pPolygon, ref PolygonSymbol);
            }
            #endregion

            #region 初始化
            Tuple<int, int> sGrid = NodeInGrid[OriginPoint];//起点格网编码
            List<Tuple<int, int>> desGrids = new List<Tuple<int, int>>();//终点格网编码
            for (int i = 0; i < DesPoints.Count; i++)
            {
                desGrids.Add(NodeInGrid[DesPoints[i]]);
            }

            cFlowMap cFM = new cFlowMap(sGrid, desGrids, PointFlow);
            //Dictionary<Tuple<int, int>, double> pWeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 3);
            Dictionary<Tuple<int, int>, double> pWeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重(这里参数需要设置)
            //Dictionary<Tuple<int, int>, double> pWeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 8, 1);//确定整个Grids的权重(这里参数需要设置)

            ///加快搜索，提前将所有探索方向全部提前计算（实际上应该是需要时才计算，这里可能导致后续计算存在重叠问题，在计算过程中解决即可）
            //Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> DesDirPt = FMU.GetDesDirPt(pWeighGrids, desGrids);//获取给定节点的方向探索路径
            //Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> DesDirPt = FMU.GetDesDirPt_2(pWeighGrids, desGrids, 1);//获取给定节点的方向探索路径【该阶段已考虑避免重叠约束】
            Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> DesDirPt = FMU.GetDesDirPt_3(GridWithNode, pWeighGrids, desGrids, Grids, 0);//考虑权重（按权重的大小添加）
            //Dictionary<Tuple<int, int>, double> DesDis = FMU.GetDisOrder(desGrids, sGrid);
            #endregion

            #region 遍历构成Flow过程
            double CountLabel = 0;//进程监控
            while (desGrids.Count > 0)
            {
                CountLabel++; Console.WriteLine(CountLabel);//进程监控
                double MaxDis = 0;
                Path CachePath = null;
                //List<Tuple<int, int>> TestTargetPath = null;//测试用
                //double TestShort = 0;//测试用

                for (int i = 0; i < desGrids.Count; i++)
                {
                    double MinLength = 100000;
                    List<Tuple<int, int>> TargetPath = null;
                    int Label = 0;//标识终点到起点最短距离的节是否是起点（=1表示终点是起点）
                    int AngleLabel = 0;//标识最终选用的路径是否非钝角流入（0钝角流入；1非钝角流入）

                    #region 需要判断该节点是否被已构建的路径覆盖(已覆盖)
                    if (FMU.IntersectGrid(desGrids[i], cFM.PathGrids))//判断该节点是否与已有路径重叠
                    {
                        TargetPath = new List<Tuple<int, int>>();
                        TargetPath.Add(desGrids[i]);
                    }
                    #endregion

                    #region 无覆盖
                    else
                    {
                        //每次更新网格权重
                        Console.Write(i);
                        double MinDis = FMU.GetMinDis(desGrids[i], cFM.PathGrids);
                        //List<double> DisList = DesDis.Values.ToList(); DisList.Sort();//升序排列
                        //double Order = DisList.IndexOf(DesDis[desGrids[i]]) / DisList.Count;

                        #region 获取到PathGrid的路径
                        Dictionary<int, PathTrace> DirPt = DesDirPt[desGrids[i]];
                        for (int j = 0; j < cFM.PathGrids.Count; j++)
                        {
                            #region 获取终点到已有路径中某点的最短路径

                            #region 不考虑方向限制
                            //List<int> DirList = new List<int>();

                            //DirList.Add(1);
                            //DirList.Add(2);
                            //DirList.Add(3);
                            //DirList.Add(4);
                            //DirList.Add(5);
                            //DirList.Add(6);
                            //DirList.Add(7);
                            //DirList.Add(8);
                            #endregion

                            List<int> DirList = FMU.GetConDirR(cFM.PathGrids[j], desGrids[i]);//获取限制性约束的方向                   
                            int DirID = FMU.GetNumber(DirList);

                            #region 可能存在重合点的情况
                            if (DirID == 0)
                            {
                                break;
                            }
                            #endregion

                            List<Tuple<int, int>> CacheShortPath = DirPt[DirID].GetShortestPath(cFM.PathGrids[j], desGrids[i]);
                            #endregion

                            #region 需要考虑可能不存在路径的情况
                            double CacheShortPathLength = 0;
                            if (CacheShortPath != null)
                            {
                                CacheShortPathLength = FMU.GetPathLength(CacheShortPath);
                            }
                            else
                            {
                                CacheShortPathLength = 10000000;
                            }
                            #endregion

                            #region 添加交叉约束
                            if (CacheShortPath != null)
                            {
                                List<Tuple<int, int>> CopydesGrids = Clone((object)desGrids) as List<Tuple<int, int>>;
                                bool OverlayLabel = FMU.IntersectPath(CacheShortPath, CopydesGrids);//判断是否重叠

                                #region 存在交叉
                                if (FMU.IntersectPathInt(CacheShortPath, cFM.PathGrids) == 2)//这里的相交修改了
                                {
                                    CacheShortPathLength = 1000000 + CacheShortPathLength;//交叉惩罚系数更高
                                }
                                #endregion

                                #region 存在重叠
                                else if (FMU.IntersectPathInt(CacheShortPath, cFM.PathGrids) == 1 || OverlayLabel)
                                {
                                    //if (MinDis < Math.Sqrt(1.9) && (CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.45) > MaxDis)///如果长度小于该值才需要判断；否则无须判断
                                    //if (MinDis < Math.Sqrt(1.9))
                                    //if (MinDis < 2.9)
                                    //{
                                    #region 考虑到搜索方向固定可能导致的重合
                                    Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝
                                    FMU.FlowCrosssingContraint(WeighGrids, 0, desGrids[i], cFM.PathGrids[j], cFM.PathGrids);//Cross约束
                                    //FMU.FlowOverLayContraint_2(desGrids, WeighGrids, 1, desGrids[i]);//Overlay约束
                                    //FMU.FlowOverLayContraint_2Tar(desGrids, WeighGrids, 0, desGrids[i]);
                                    FMU.FlowOverLayContraint_3Tar(desGrids, GridWithNode, WeighGrids, 0, desGrids[i], Grids, true);//0就是1阶；0阶时该段代码注释掉即可

                                    PathTrace Pt = new PathTrace();
                                    List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                                    JudgeList.Add(desGrids[i]);//添加搜索的起点
                                    Pt.MazeAlg(JudgeList, WeighGrids, 1, DirList);//备注：每次更新以后,WeightGrid会清零  
                                    CacheShortPath = Pt.GetShortestPath(cFM.PathGrids[j], desGrids[i]);

                                    if (CacheShortPath != null)
                                    {
                                        CacheShortPathLength = FMU.GetPathLength(CacheShortPath);

                                        #region 判段交叉或重合
                                        if (FMU.IntersectPathInt(CacheShortPath, cFM.PathGrids) == 2 || FMU.IntersectPath(CacheShortPath, CopydesGrids))
                                        {
                                            CacheShortPathLength = 1000000 + CacheShortPathLength;//交叉惩罚系数更高
                                        }
                                        #endregion
                                    }
                                    else
                                    {
                                        CacheShortPathLength = 10000000;
                                    }
                                    #endregion
                                    //}

                                    //否则，加上一个极大值
                                    //else
                                    //{
                                    //    CacheShortPathLength = 100000 + CacheShortPathLength;
                                    //}
                                }
                                #endregion

                                //if (FMU.IntersectPath(CacheShortPath, cFM.PathGrids))
                                //{
                                //    CacheShortPathLength = 100000 + CacheShortPathLength;
                                //}

                                //if (FMU.LineIntersectPath(CacheShortPath, cFM.PathGrids, Grids))
                                //{
                                //    CacheShortPathLength = 1000000 + CacheShortPathLength;
                                //}

                                //if (FMU.obstacleIntersectPath(CacheShortPath, Features, Grids))
                                //{
                                //    CacheShortPathLength = 1000000 + CacheShortPathLength;
                                //}

                            }
                            #endregion

                            #region 添加角度约束限制
                            if (CacheShortPath != null)
                            {
                                if (FMU.AngleContraint(CacheShortPath, cFM.GridForPaths[cFM.PathGrids[j]], Grids))
                                {
                                    CacheShortPathLength = 15 + CacheShortPathLength;
                                }
                            }
                            #endregion

                            #region 添加长度约束
                            //if (CacheShortPathLength < Math.Sqrt(1.9))
                            //if (CacheShortPathLength < 2.9)
                            //{
                            //CacheShortPathLength = 15 + CacheShortPathLength;
                            //}
                            #endregion

                            double TotalLength = 0;
                            TotalLength = CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.65;
                            #region 比较获取某给定节点到起点的最短路径
                            if (TotalLength < MinLength)
                            {
                                if (cFM.GridForPaths[cFM.PathGrids[j]].Length == 0 && !cFM.PathGrids.Contains(CacheShortPath[1]))//消除某些点到起点的最短路径是经过已有路径的情况
                                {
                                    Label = 1;//标识最短路径终点是起点
                                }

                                else
                                {
                                    Label = 0;//标识最短路径终点非起点
                                }

                                MinLength = TotalLength;
                                List<Tuple<int, int>> pCachePath = cFM.GridForPaths[cFM.PathGrids[j]].ePath.ToList();
                                CacheShortPath.RemoveAt(0);//移除第一个要素，避免存在重复元素

                                pCachePath.AddRange(CacheShortPath);
                                TargetPath = pCachePath;

                                //if (FMU.AngleContraint(TargetPath, cFM.GridForPaths[cFM.PathGrids[j]], Grids))
                                //{
                                //    AngleLabel = 1;//非钝角流入
                                //}
                                //else
                                //{
                                //    AngleLabel = 0;//钝角流入
                                //}

                                //TestTargetPath = CacheShortPath;//测试用
                                //TestShort = CacheShortPathLength;//测试用
                            }
                            #endregion
                        }
                        #endregion
                    }
                    //}
                    #endregion

                    #region 表示起点优先限制
                    //if (Label == 1)
                    //{
                    //    MinLength = MinLength + 10000;
                    //}
                    #endregion

                    #region 添加角度约束【特殊情况-一般不需要考虑该情况】
                    //if (AngleLabel==1)
                    //{
                    //    MinLength = MinLength - 10000;
                    //}
                    #endregion

                    #region 获取到起点路径最长终点的路径
                    if (TargetPath == null)//可能存在路径为空的情况
                    {
                        MinLength = 0;
                    }

                    if (MinLength > MaxDis)
                    {
                        MaxDis = MinLength;
                        CachePath = new Path(sGrid, desGrids[i], TargetPath);

                        //double Test = FMU.GetPathLength(TestTargetPath);//测试用
                        //int TesTloc = 0;
                    }
                    #endregion
                }

                #region 可视化显示（表示Path的生成过程）
                FlowDraw FD1 = new FlowDraw(pMapControl, Grids);
                FD1.FlowPathDraw(CachePath, 1, 0);
                #endregion

                #region 防止可能出现空的情况
                //cFM.PathRefresh(CachePath, 1);
                if (CachePath != null)
                {
                    cFM.PathRefresh(CachePath, 1, PointFlow[GridWithNode[CachePath.endPoint]]);//更新：包括子路径与流量更新
                    desGrids.Remove(CachePath.endPoint);//移除一个Destination
                    //DesDis.Remove(CachePath.endPoint);
                }

                else
                {
                    desGrids.RemoveAt(0);
                }
                #endregion
            }
            #endregion

            #region 可视化和输出(TVCG修改前)
            FlowDraw FD2 = new FlowDraw(pMapControl, Grids, AllPoints, NodeInGrid, GridWithNode, OutFilePath);
            //FD2.SmoothFlowMap(cFM.SubPaths, 2, 2000, 20, 1, 1, 1, 0, 200);
            //FD2.FlowMapDraw(cFM.SubPaths, 15, 2000, 20, 1, 1, 1);
            //FD2.FlowMapDraw(cFM.SubPaths, 15, 2000, 20, 1, 0, 1);
            #endregion

            #region 可视化和输出(8.22 TVCG修改)
            //FlowDraw FD2 = new FlowDraw(pMapControl, Grids, AllPoints, NodeInGrid, GridWithNode, OutFilePath);
            //FD2.SmoothFlowMap_2(cFM.SubPaths, 4, 0.05, 2000, MinFlow, 2, 1, 0.4, 0.1 * GridXY[0], 1, 200, 0, 0.001, true, true, 0);//三类边控制点的偏移距离为格网长度的0.1
            FD2.FlowMapDraw(cFM.SubPaths, 2, 0.05, 2000, 20, 1, 1, 1);//Grid Connect Layout
            FD2.FlowMapDraw(cFM.SubPaths, 2, 0.05, 2000, 20, 1, 0, 1);//Point Connect Layout
            #endregion
        }

        /// <summary>
        /// 将建筑物转化为IPolygon
        /// </summary>
        /// <param name="pPolygonObject"></param>
        /// <returns></returns>
        IPolygon PolygonObjectConvert(PolygonObject pPolygonObject)
        {
            Ring ring1 = new RingClass();
            object missing = Type.Missing;

            IPoint curResultPoint = new PointClass();
            TriNode curPoint = null;
            if (pPolygonObject != null)
            {
                for (int i = 0; i < pPolygonObject.PointList.Count; i++)
                {
                    curPoint = pPolygonObject.PointList[i];
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    ring1.AddPoint(curResultPoint, ref missing, ref missing);
                }
            }

            curPoint = pPolygonObject.PointList[0];
            curResultPoint.PutCoords(curPoint.X, curPoint.Y);
            ring1.AddPoint(curResultPoint, ref missing, ref missing);

            IGeometryCollection pointPolygon = new PolygonClass();
            pointPolygon.AddGeometry(ring1 as IGeometry, ref missing, ref missing);
            IPolygon pPolygon = pointPolygon as IPolygon;

            //PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
            //object PolygonSymbol = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 0, 20, 20);

            //pMapControl.DrawShape(pPolygon, ref PolygonSymbol);
            //pMapControl.Map.RecalcFullExtent();

            pPolygon.SimplifyPreserveFromTo();
            return pPolygon;
        }

        /// <summary>
        /// GetConcave
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e)
        {
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox2.Text != null)
            {
                IFeatureLayer PointLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(PointLayer);
            }
            #endregion

            #region 创建凸包
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();
            SMap map2 = new SMap();

            ConvexNull cn = new ConvexNull(map.PointList);
            cn.CreateConvexNull();
            PolygonObject cPolygon = new PolygonObject(0, cn.ConvexVertexSet);
            map2.PolygonList.Add(cPolygon);
            #endregion

            #region 输出
            if (OutFilePath != null) { map2.WriteResult2Shp(OutFilePath, pMap.SpatialReference); }
            #endregion
        }

        /// <summary>
        ///生成AlphaShpae 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button8_Click(object sender, EventArgs e)
        {
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();
            #endregion

            #region AlphaShape
            DelaunayTin dt = new DelaunayTin(map.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
            List<TriEdge> EdgeList=dt.CreateAlphaShape2(8);

            //dt.TriEdgeList = EdgeList;
            //dt.WriteShp(OutFilePath, pMap.SpatialReference);

            List<PolygonObject> PoList = dt.GetAlphaShape(EdgeList);
            map.PolygonList = PoList;
            map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            #endregion

        }

        /// <summary>
        /// 计算Population
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button9_Click(object sender, EventArgs e)
        {
            #region OutPut Check
            if (OutFilePath == null)
            {
                MessageBox.Show("Please give the OutPut path");
                return;
            }
            #endregion

            #region OD参数
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);           
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            IFeatureLayer FlowFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox5.Text);
            IFeatureClass FlowFeatureClass = FlowFeatureLayer.FeatureClass;

            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            FMU.GetOD(pFeatureClass, OriginPoint, DesPoints, AllPoints, PointFlow, pMap.SpatialReference);
            #endregion

            #region 获取Grids和节点编码
            IRasterLayer rLayer = pFeatureHandle.GetRasterLayer(pMap, this.comboBox1.Text);//获取图层
            IRasterBandCollection rasBandCol = (IRasterBandCollection)rLayer.Raster;
            IRawBlocks rawBlocks = (IRawBlocks)rasBandCol.Item(0);
            IRasterInfo rasInfo = rawBlocks.RasterInfo;
            double X = rasInfo.CellSize.X; double Y = rasInfo.CellSize.Y;
            //int rowNum = (int)Math.Ceiling(rasInfo.Height / Y); int colNum = (int)Math.Ceiling(rasInfo.Width / X);
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2]; GridXY[0] = X; GridXY[1] = Y;
            double[] ExtendValue = new double[4];
            ExtendValue[0] = rasInfo.Extent.XMin; ExtendValue[1] = rasInfo.Extent.YMin; ExtendValue[2] = rasInfo.Extent.XMax; ExtendValue[3] = rasInfo.Extent.YMax;
            ///Grids的排列是从左下角开始，从左往右，从下往上
            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGridNonExtend(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网

            #region 考虑边界的格网构建
            //IFeatureLayer CacheLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
            //List<IFeatureLayer> LayerList = new List<IFeatureLayer>();
            //LayerList.Add(CacheLayer);
            //List<Tuple<IGeometry, esriGeometryType>> Features = FMU.GetFeatures(LayerList, pMap.SpatialReference);
            //Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetInGrid(CacheGrids, Features, 0.02);
            #endregion

            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）
            #endregion

            #region 输出网格
            SMap OutMap = new SMap();
            object PolygonSymbol = Sb.PolygonSymbolization(0.4, 153, 153, 153, 0, 0, 20, 20);
            foreach (KeyValuePair<Tuple<int, int>, List<double>> Kv in Grids)
            {
                List<TriNode> NodeList = new List<TriNode>();

                TriNode Node1 = new TriNode();
                Node1.X = Kv.Value[0];
                Node1.Y = Kv.Value[1];

                TriNode Node2 = new TriNode();
                Node2.X = Kv.Value[2];
                Node2.Y = Kv.Value[1];

                TriNode Node3 = new TriNode();
                Node3.X = Kv.Value[2];
                Node3.Y = Kv.Value[3];

                TriNode Node4 = new TriNode();
                Node4.X = Kv.Value[0];
                Node4.Y = Kv.Value[3];

                NodeList.Add(Node1); NodeList.Add(Node2); NodeList.Add(Node3); NodeList.Add(Node4);

                TriNode MidNode = new TriNode();
                MidNode.X = (Kv.Value[0] + Kv.Value[2]) / 2;
                MidNode.Y = (Kv.Value[1] + Kv.Value[3]) / 2;
                PointObject CachePoint = new PointObject(0, MidNode);

                PolygonObject CachePo = new PolygonObject(0, NodeList);
                OutMap.PolygonList.Add(CachePo);
                OutMap.PointList.Add(CachePoint);

                IPolygon pPolygon = this.PolygonObjectConvert(CachePo);
                pMapControl.DrawShape(pPolygon, ref PolygonSymbol);
            }
            #endregion

            #region 网格赋值
            IPixelBlock pb = rawBlocks.CreatePixelBlock();
            rawBlocks.ReadBlock(0, 0, 0, pb);// Get the pixel block.
            System.Array safeArray;
            safeArray = (System.Array)pb.get_SafeArray(0);//// Put the pixel block into a SafeArray for manipulation.能读取所有属性

            ///注意：我们以行列标识；RawBlock以XY标识
            Dictionary<Tuple<int, int>, double> ValueGrids = new Dictionary<Tuple<int, int>, double>();
            for (int i = 0; i < rowNum; i++)
            {
                for (int j = 0; j < colNum; j++)
                {
                    Tuple<int, int> CacheGrid = new Tuple<int, int>(i, j);
                    if (Grids.ContainsKey(CacheGrid))
                    {
                        #region 以人口编码
                        double Value = Convert.ToDouble(safeArray.GetValue(j, rowNum - i - 1));///注意不同的编码规则
                        Console.WriteLine(Value);
                        ValueGrids.Add(CacheGrid, Value);
                        #endregion
                    }
                }
            }
            #endregion

            #region 初始化
            //Tuple<int, int> sGrid = NodeInGrid[OriginPoint];//起点格网编码
            //List<Tuple<int, int>> desGrids = new List<Tuple<int, int>>();//终点格网编码
            //for (int i = 0; i < DesPoints.Count; i++)
            //{
            //    desGrids.Add(NodeInGrid[DesPoints[i]]);
            //}

            //cFlowMap cFM = new cFlowMap(sGrid, desGrids, PointFlow);
            Dictionary<Tuple<int, int>, double> pWeighGrids = ValueGrids;//确定整个Grids的权重(这里参数需要设置)[每个格网的权重以人口密度代替]           
            #endregion

            #region 计算相交
            Dictionary<Tuple<int, int>, List<double>> OutGrids = Clone((object)Grids) as Dictionary<Tuple<int, int>, List<double>>;

            double PopSum = 0; int PopCount = 0; double Count = 0;
            int GridCount = Grids.Count;
            #region 判断过程
            foreach (KeyValuePair<Tuple<int, int>, List<double>> kv in Grids)
            {
                Count++;
                #region 网格范围
                Ring ring1 = new RingClass();
                object missing = Type.Missing;

                IPoint curResultPoint1 = new PointClass();
                IPoint curResultPoint2 = new PointClass();
                IPoint curResultPoint3 = new PointClass();
                IPoint curResultPoint4 = new PointClass();

                curResultPoint1.PutCoords(kv.Value[0], kv.Value[1]);
                curResultPoint2.PutCoords(kv.Value[2], kv.Value[1]);
                curResultPoint3.PutCoords(kv.Value[2], kv.Value[3]);
                curResultPoint4.PutCoords(kv.Value[0], kv.Value[3]);

                ring1.AddPoint(curResultPoint1, ref missing, ref missing);
                ring1.AddPoint(curResultPoint4, ref missing, ref missing);
                ring1.AddPoint(curResultPoint3, ref missing, ref missing);
                ring1.AddPoint(curResultPoint2, ref missing, ref missing);
                ring1.AddPoint(curResultPoint1, ref missing, ref missing);

                IGeometryCollection pointPolygon = new PolygonClass();
                pointPolygon.AddGeometry(ring1 as IGeometry, ref missing, ref missing);
                IPolygon pPolygon = pointPolygon as IPolygon;

                IArea pArea = pPolygon as IArea;
                #endregion

                ITopologicalOperator iTo = pPolygon as ITopologicalOperator;
                IRelationalOperator iRo = pPolygon as IRelationalOperator;

                #region 判断区域交叉
                bool GridValid = false;
                if (FlowFeatureClass.FeatureCount(null) > 0)
                {
                    for (int j = 0; j < FlowFeatureClass.FeatureCount(null); j++)
                    {
                        IFeature pFeature = FlowFeatureClass.GetFeature(j);

                        if (iRo.Overlaps(pFeature.Shape) || iRo.Within(pFeature.Shape) || iRo.Crosses(pFeature.Shape))
                        {
                            GridValid = true;
                            PopCount++;
                            PopSum = PopSum + pWeighGrids[kv.Key];
                            break;
                        }
                    }
                }
                #endregion
            }
            #endregion

            double AvePop = PopSum / PopCount;
            int testloc = 0;
            #endregion
        }
    }
}
