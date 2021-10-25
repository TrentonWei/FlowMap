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
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.Geoprocessing;
using AuxStructureLib;
using AuxStructureLib.IO;
using PrDispalce.FlowMap;

namespace PrDispalce.CGFlowMap
{
    public partial class CGFlowMap : Form
    {
        public CGFlowMap(AxMapControl axMapControl)
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
        private void CGFlowMap_Load(object sender, EventArgs e)
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
                    this.comboBox1.Items.Add(strLayerName);
                }
            }
            #endregion

            #region 默认显示第一个
            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
            }
            #endregion
        }


        /// <summary>
        /// FlowMapGeneralization
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            #region OutPut check
            if (OutFilePath==null)
            {
                MessageBox.Show("Please give the OutPut path");
                return;
            }
            #endregion

            #region OD参数
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            //IFeatureLayer CacheLayer = pFeatureHandle.GetLayer(pMap, this.comboBox3.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            FMU.GetOD(pFeatureClass, OriginPoint, DesPoints, AllPoints, PointFlow);
            #endregion

            #region 获取Grids和节点编码
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            //GridXY[0] = 0.8; GridXY[1] = 0.8;
            GridXY = Fs.GetXY(AllPoints, 2, 0.05);
            double[] ExtendValue = FMU.GetExtend(pFeatureLayer);
            //List<IFeatureLayer> LayerList = new List<IFeatureLayer>();
            //LayerList.Add(CacheLayer);
            //List<Tuple<IGeometry, esriGeometryType>> Features = FMU.GetFeatures(LayerList);
            //Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGridConObstacle(ExtendValue, GridXY, Features, ref colNum, ref rowNum, 0);//构建格网

            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网     
            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）

            Tuple<int, int> sGrid = NodeInGrid[OriginPoint];//起点格网编码
            List<Tuple<int, int>> desGrids = new List<Tuple<int, int>>();//终点格网编码
            for (int i = 0; i < DesPoints.Count; i++)
            {
                desGrids.Add(NodeInGrid[DesPoints[i]]);
            }

            cFlowMap cFM = new cFlowMap(sGrid, desGrids, PointFlow);
            Dictionary<Tuple<int, int>, double> pWeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重(这里参数需要设置)
            //Dictionary<Tuple<int, int>, double> pWeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 8, 1);//确定整个Grids的权重(这里参数需要设置)

            ///加快搜索，提前将所有探索方向全部提前计算（实际上应该是需要时才计算，这里可能导致后续计算存在重叠问题，在计算过程中解决即可）
            Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> DesDirPt = FMU.GetDesDirPt(pWeighGrids, desGrids);//获取给定节点的方向探索路径
            //Dictionary<Tuple<int, int>, double> DesDis = FMU.GetDisOrder(desGrids, sGrid);
            #endregion

            #region 遍历构成Flow过程
            double CountLabel = 0;//进程监控
            while (desGrids.Count > 0)
            {
                CountLabel++; Console.WriteLine(CountLabel);//进程监控
                double MaxDis = 0;
                PrDispalce.FlowMap.Path CachePath = null;
                List<Tuple<int, int>> TestTargetPath = null;//测试用
                double TestShort = 0;//测试用

                for (int i = 0; i < desGrids.Count; i++)
                {
                    //每次更新网格权重
                    Console.Write(i);
                    double MinDis = FMU.GetMinDis(desGrids[i], cFM.PathGrids);
                    //List<double> DisList = DesDis.Values.ToList(); DisList.Sort();//升序排列
                    //double Order = DisList.IndexOf(DesDis[desGrids[i]]) / DisList.Count;

                    #region 获取到PathGrid的路径
                    double MinLength = 100000;
                    List<Tuple<int, int>> TargetPath = null;
                    int Label = 0;//标识终点到起点最短距离的节是否是起点（=1表示终点是起点）
                    Dictionary<int, PathTrace> DirPt = DesDirPt[desGrids[i]];

                    for (int j = 0; j < cFM.PathGrids.Count; j++)
                    {
                        #region 优化搜索速度（只搜索制定范围内的节点）
                        //if (FMU.RLJudgeGrid(desGrids[i], cFM.PathGrids[j], sGrid))
                        //{
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
                            #region 存在交叉
                            if (FMU.IntersectPathInt(CacheShortPath, cFM.PathGrids) == 2)//这里的相交修改了
                            {
                                CacheShortPathLength = 1000000 + CacheShortPathLength;//交叉惩罚系数更高
                            }
                            #endregion

                            #region 存在重叠
                            else if (FMU.IntersectPathInt(CacheShortPath, cFM.PathGrids) == 1)
                            {
                                //if (MinDis < Math.Sqrt(1.9) && (CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.65) > MaxDis)///如果长度小于该值才需要判断；否则无须判断
                                if (MinDis < Math.Sqrt(1.9))
                                //if (MinDis < 2.9)
                                {
                                    #region 考虑到搜索方向固定可能导致的重合
                                    Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝
                                    FMU.FlowCrosssingContraint(desGrids, WeighGrids, 0, desGrids[i], cFM.PathGrids[j], cFM.PathGrids);//Overlay约束
                                    PathTrace Pt = new PathTrace();
                                    List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                                    JudgeList.Add(desGrids[i]);//添加搜索的起点
                                    Pt.MazeAlg(JudgeList, WeighGrids, 1, DirList);//备注：每次更新以后,WeightGrid会清零  
                                    CacheShortPath = Pt.GetShortestPath(cFM.PathGrids[j], desGrids[i]);

                                    if (CacheShortPath != null)
                                    {
                                        CacheShortPathLength = FMU.GetPathLength(CacheShortPath);

                                        #region 判段交叉
                                        if (FMU.IntersectPathInt(CacheShortPath, cFM.PathGrids) == 2)
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
                                }

                                //否则，加上一个极大值
                                else
                                {
                                    CacheShortPathLength = 100000 + CacheShortPathLength;
                                }
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
                        if (CacheShortPathLength < Math.Sqrt(1.9))
                        //if (CacheShortPathLength < 2.9)
                        {
                            CacheShortPathLength = 15 + CacheShortPathLength;
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

                            TestTargetPath = CacheShortPath;//测试用
                            TestShort = CacheShortPathLength;//测试用
                        }
                        #endregion
                    }
                        #endregion
                    //}
                    #endregion

                    #region 表示起点优先限制
                    if (Label == 1)
                    {
                        MinLength = MinLength + 10000;
                    }
                    #endregion

                    #region 获取到起点路径最长终点的路径
                    if (TargetPath == null)//可能存在路径为空的情况
                    {
                        MinLength = 0;
                    }

                    if (MinLength > MaxDis)
                    {
                        MaxDis = MinLength;
                        CachePath = new PrDispalce.FlowMap.Path(sGrid, desGrids[i], TargetPath);

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

            #region 可视化和输出
            FlowDraw FD2 = new FlowDraw(pMapControl, Grids, AllPoints, NodeInGrid, GridWithNode, OutFilePath);
            FD2.SmoothFlowMap(cFM.SubPaths, 2, 2000, 20, 1, 1, 1, 0, 200);
            FD2.FlowMapDraw(cFM.SubPaths, 15, 2000, 20, 1, 1, 1);
            FD2.FlowMapDraw(cFM.SubPaths, 15, 2000, 20, 1, 0, 1);
            #endregion
        }

        /// <summary>
        /// OutPut
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
            this.comboBox2.Text = OutFilePath;
        }
    
    }
}
