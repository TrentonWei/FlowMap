using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

namespace PrDispalce.FlowMap
{
    /// <summary>
    /// Flow的绘制
    /// </summary>
    class FlowDraw
    {
        #region 参数
        AxMapControl pMapControl;//绘制控件
        Dictionary<Tuple<int, int>, List<double>> Grids = new Dictionary<Tuple<int, int>, List<double>>();//控件格网划分
        List<IPoint> AllPoints = new List<IPoint>();//ODPoints
        PrDispalce.FlowMap.Symbolization Sb = new Symbolization();//可视化测试
        Dictionary<IPoint, Tuple<int, int>> NodeInGrid=new Dictionary<IPoint,Tuple<int,int>>() ;//获取点对应的格网
        string OutPath;
        Dictionary<Tuple<int, int>, IPoint> GridWithNode=new Dictionary<Tuple<int,int>,IPoint>();//获取格网中的点（每个格网最多对应一个点）
        #endregion

        #region 构造函数
        public FlowDraw()
        {

        }

        public FlowDraw(AxMapControl pMapControl, Dictionary<Tuple<int, int>, List<double>> Grids)
        {
            this.pMapControl = pMapControl;
            this.Grids = Grids;
        }

        public FlowDraw(AxMapControl pMapControl, string OutPath)
        {
            this.pMapControl = pMapControl;
            this.OutPath = OutPath;
        }

        public FlowDraw(AxMapControl pMapControl, Dictionary<Tuple<int, int>, List<double>> Grids, List<IPoint> AllPoints,Dictionary<IPoint, Tuple<int, int>> NodeInGrid, Dictionary<Tuple<int, int>, IPoint> GridWithNode)
        {
            this.pMapControl = pMapControl;
            this.Grids = Grids;
            this.AllPoints = AllPoints;
            this.NodeInGrid=NodeInGrid;
            this.GridWithNode=GridWithNode;
        }

        public FlowDraw(AxMapControl pMapControl, Dictionary<Tuple<int, int>, List<double>> Grids, List<IPoint> AllPoints, Dictionary<IPoint, Tuple<int, int>> NodeInGrid, Dictionary<Tuple<int, int>, IPoint> GridWithNode,string OutPath)
        {
            this.pMapControl = pMapControl;
            this.Grids = Grids;
            this.AllPoints = AllPoints;
            this.NodeInGrid = NodeInGrid;
            this.GridWithNode = GridWithNode;
            this.OutPath=OutPath;
        }
        #endregion

        /// <summary>
        /// Darw a Path
        /// </summary>
        /// <param name="CachePath">FlowPath</param>
        /// <param name="Width">给定宽度</param>
        /// <param name="Type">ODType=1不考虑ODPoints绘制；ODType=1，考虑ODPoints绘制</param>
        /// OutType是否输出 =0；不输出 =1输出
        public void FlowPathDraw(Path CachePath, double Width, int ODType, int OutType, out PolylineObject CachePoLine)
        {           
            object cPolylineSb = Sb.LineSymbolization(Width, 100, 100, 100, 0);

            #region CachePath共线
            if (this.OnLine(CachePath))
            {
                List<TriNode> NodeList = new List<TriNode>();

                IPoint sPoint = new PointClass();
                IPoint ePoint = new PointClass();

                #region 不考虑ODPoints
                sPoint.X = (Grids[CachePath.ePath[0]][0] + Grids[CachePath.ePath[0]][2]) / 2;
                sPoint.Y = (Grids[CachePath.ePath[0]][1] + Grids[CachePath.ePath[0]][3]) / 2;

                ePoint.X = (Grids[CachePath.ePath[CachePath.ePath.Count - 1]][0] + Grids[CachePath.ePath[CachePath.ePath.Count - 1]][2]) / 2;
                ePoint.Y = (Grids[CachePath.ePath[CachePath.ePath.Count - 1]][1] + Grids[CachePath.ePath[CachePath.ePath.Count - 1]][3]) / 2;
                #endregion

                #region 考虑ODPoints
                if (ODType == 1)
                {
                    if (GridWithNode.Keys.Contains(CachePath.ePath[0]))
                    {
                        sPoint.X = GridWithNode[CachePath.ePath[0]].X;
                        sPoint.Y = GridWithNode[CachePath.ePath[0]].Y; ;
                    }

                    if (GridWithNode.Keys.Contains(CachePath.ePath[CachePath.ePath.Count - 1]))
                    {
                        ePoint.X = GridWithNode[CachePath.ePath[CachePath.ePath.Count - 1]].X;
                        ePoint.Y = GridWithNode[CachePath.ePath[CachePath.ePath.Count - 1]].Y;
                    }
                }
                #endregion

                #region 如果需要输出
                if (OutType == 1)
                {
                    TriNode CacheNode1 = new TriNode();
                    CacheNode1.X = sPoint.X;
                    CacheNode1.Y = sPoint.Y;
                    NodeList.Add(CacheNode1);

                    TriNode CacheNode2 = new TriNode();
                    CacheNode2.X = ePoint.X;
                    CacheNode2.Y = ePoint.Y;
                    NodeList.Add(CacheNode2);
                }           
                #endregion   
                
                IPolyline iLine = new PolylineClass();
                iLine.FromPoint = sPoint; iLine.ToPoint = ePoint;
                //pMapControl.DrawShape(iLine, ref cPolylineSb);
                CachePoLine = new PolylineObject(NodeList);
            }
            #endregion

            #region CachePath不是一条直线
            else
            {
                List<TriNode> NodeList = new List<TriNode>();
                for (int i = 0; i < CachePath.ePath.Count - 1; i++)
                {
                    IPoint sPoint = new PointClass();
                    IPoint ePoint = new PointClass();

                    #region 不考虑ODPoints
                    sPoint.X = (Grids[CachePath.ePath[i]][0] + Grids[CachePath.ePath[i]][2]) / 2;
                    sPoint.Y = (Grids[CachePath.ePath[i]][1] + Grids[CachePath.ePath[i]][3]) / 2;

                    ePoint.X = (Grids[CachePath.ePath[i + 1]][0] + Grids[CachePath.ePath[i + 1]][2]) / 2;
                    ePoint.Y = (Grids[CachePath.ePath[i + 1]][1] + Grids[CachePath.ePath[i + 1]][3]) / 2;
                    #endregion

                    #region 考虑ODPoints
                    if (ODType == 1)
                    {
                        if (GridWithNode.Keys.Contains(CachePath.ePath[i]))
                        {
                            sPoint.X = GridWithNode[CachePath.ePath[i]].X;
                            sPoint.Y = GridWithNode[CachePath.ePath[i]].Y; ;
                        }

                        if (GridWithNode.Keys.Contains(CachePath.ePath[i + 1]))
                        {
                            ePoint.X = GridWithNode[CachePath.ePath[i + 1]].X;
                            ePoint.Y = GridWithNode[CachePath.ePath[i + 1]].Y;
                        }
                    }
                    #endregion

                    #region 如果需要输出
                    if (OutType == 1)
                    {
                        TriNode CacheNode1 = new TriNode();
                        CacheNode1.X = sPoint.X;
                        CacheNode1.Y = sPoint.Y;
                        NodeList.Add(CacheNode1);

                        if (i == CachePath.ePath.Count - 2)
                        {
                            TriNode CacheNode2 = new TriNode();
                            CacheNode2.X = ePoint.X;
                            CacheNode2.Y = ePoint.Y;
                            NodeList.Add(CacheNode2);
                        }
                    }
                    #endregion

                    IPolyline iLine = new PolylineClass();
                    iLine.FromPoint = sPoint; iLine.ToPoint = ePoint;

                    //pMapControl.DrawShape(iLine, ref cPolylineSb);
                }

                CachePoLine = new PolylineObject(NodeList);
            }
            #endregion

            CachePoLine.Volume = CachePath.Volume;
            CachePoLine.GetLength();//计算长度
            //CachePoLine.SmoothValue = PU.GetSmoothValue(CachePoLine);
            CachePoLine.FlowIn = CachePath.FlowInPath.Count;
            CachePoLine.FlowOut = CachePath.FlowOutPath.Count;        
        }

        /// <summary>
        /// Darw a Path
        /// </summary>
        /// <param name="CachePath">FlowPath</param>
        /// <param name="Width">给定宽度</param>
        /// <param name="Type">ODType=1不考虑ODPoints绘制；ODType=1，考虑ODPoints绘制</param>
        /// OutType是否输出 =0；不输出 =1输出
        public void FlowPathDraw(Path CachePath, double Width, int ODType)
        {
            if (CachePath != null && CachePath.ePath != null)
            {
                object cPolylineSb = Sb.LineSymbolization(Width, 51, 51, 51, 0);

                List<TriNode> NodeList = new List<TriNode>();
                for (int i = 0; i < CachePath.ePath.Count - 1; i++)
                {
                    IPoint sPoint = new PointClass();
                    IPoint ePoint = new PointClass();

                    #region 不考虑ODPoints
                    sPoint.X = (Grids[CachePath.ePath[i]][0] + Grids[CachePath.ePath[i]][2]) / 2;
                    sPoint.Y = (Grids[CachePath.ePath[i]][1] + Grids[CachePath.ePath[i]][3]) / 2;

                    ePoint.X = (Grids[CachePath.ePath[i + 1]][0] + Grids[CachePath.ePath[i + 1]][2]) / 2;
                    ePoint.Y = (Grids[CachePath.ePath[i + 1]][1] + Grids[CachePath.ePath[i + 1]][3]) / 2;
                    #endregion

                    #region 考虑ODPoints
                    if (ODType == 1)
                    {
                        if (GridWithNode.Keys.Contains(CachePath.ePath[i]))
                        {
                            sPoint.X = GridWithNode[CachePath.ePath[i]].X;
                            sPoint.Y = GridWithNode[CachePath.ePath[i]].Y; ;
                        }

                        if (GridWithNode.Keys.Contains(CachePath.ePath[i + 1]))
                        {
                            ePoint.X = GridWithNode[CachePath.ePath[i + 1]].X;
                            ePoint.Y = GridWithNode[CachePath.ePath[i + 1]].Y;
                        }
                    }
                    #endregion

                    IPolyline iLine = new PolylineClass();
                    iLine.FromPoint = sPoint; iLine.ToPoint = ePoint;

                    pMapControl.DrawShape(iLine, ref cPolylineSb);
                }
            }
        }

        /// <summary>
        /// DrawAFlowMap(按给定宽度绘制)
        /// </summary>
        /// <param name="SubPaths">FlowMap</param>
        /// <param name="Width">指定宽度</param>
        /// <param name="ODType">是否考虑ODPoint Type=0不考虑；Type=1考虑</param>
        /// OutType是否输出: =0；不输出 =1输出
        public void FlowMapDraw(List<Path> SubPaths,double Width,int ODType,int OutType)
        {
            SMap OutMap = new SMap();

            foreach (Path Pa in SubPaths)
            {
                PolylineObject CacheLine;
                this.FlowPathDraw(Pa, Width, ODType,OutType,out CacheLine);
                CacheLine.Volume = Pa.Volume;

                #region 需要输出
                if (OutType == 1)
                {
                    OutMap.PolylineList.Add(CacheLine);
                }
                #endregion
            }

            #region 需要输出
            if (OutType == 1 && OutPath != null)
            {
                OutMap.WriteResult2Shp(OutPath, pMapControl.Map.SpatialReference);
            }
            #endregion
        }

        /// <summary>
        /// 宽度按照给定参数变化绘制FlowMap
        /// </summary>
        /// <param name="SubPaths">FlowMap</param>
        /// <param name="MaxWidth">最大宽度</param>
        /// <param name="MaxVolume">最大流量</param>
        /// <param name="MinVolume">最小流量</param>
        /// <param name="Type">Type=0宽度三角函数变化；Type=1宽度近似线性变化；Type=2宽度线性变化；
        /// <param name="ODType">是否考虑ODPoint Type=0不考虑；Type=1考虑</param>
        /// OutType是否输出 =0；不输出 =1输出
        public void FlowMapDraw(List<Path> SubPaths, double MaxWidth, double MinWidth,double MaxVolume, double MinVolume, int Type,int ODType,int OutType)
        {
            SMap OutMap = new SMap();
            SMap OutEdgeMap = new SMap();
            FlowMapUtil FMU = new FlowMapUtil();

            foreach (Path Pa in SubPaths)
            {
                #region 获取路径的宽度
                double CacheWidth = this.GetWidth(Pa,MaxWidth, MinWidth, MaxVolume, MinVolume, Type);
                PolylineObject CacheLine;
                this.FlowPathDraw(Pa, CacheWidth, ODType,OutType,out CacheLine);

                #region 添加偏移距离
                int ShiftOri = 2;//2不偏移；1向左偏移；3向右偏移(在此)
                double ShiftDis = FMU.FlowPathShiftDis(Pa, Grids, out ShiftOri, MaxWidth, MinWidth, MaxVolume, MinVolume, Type, 0);
                if (ShiftOri == 1)
                {
                    CacheLine.ShiftDis = ShiftDis;//如果大于0，向左偏移

                }
                else if (ShiftOri == 3)
                {
                    CacheLine.ShiftDis = -1 * ShiftDis;//如果小于0，向左偏移
                }
                #endregion

                #region 添加路径编号
                if (Pa.FlowOutPath.Count > 0)//标识每条路径流出的路径编号
                {
                    CacheLine.FlowOutId = SubPaths.IndexOf(Pa.FlowOutPath[0]);
                }
                #endregion
                #endregion

                #region 需要输出
                if (OutType == 1)
                {
                    OutMap.PolylineList.Add(CacheLine);                  
                }
                #endregion
            }

            #region 需要输出
            if (OutType == 1 && OutPath != null && ODType == 1)
            {
                OutMap.WriteResult2Shp(OutPath, pMapControl.Map.SpatialReference, 0);
            }

            if (OutType == 1 && OutPath != null && ODType == 0)
            {
                OutMap.WriteResult2Shp(OutPath, pMapControl.Map.SpatialReference, 2);               
            }
            #endregion
        }

        /// <summary>
        /// 宽度按照给定参数变化绘制SmoothFlowMap
        /// </summary>
        /// <param name="SubPaths">FlowMap</param>
        /// <param name="MaxWidth">最大宽度</param>
        /// <param name="MaxVolume">最大流量</param>
        /// <param name="MinVolume">最小流量</param>
        /// <param name="Type">Type=0宽度三角函数变化；=1宽度线性变化</param>
        /// <param name="ODType">是否考虑ODPoint Type=0不考虑；Type=1考虑</param>
        /// OutType是否输出 =0；不输出 =1输出
        /// BeType =0 考虑直线绘制（freePath添加控制点，以贝塞尔直线生成）；=1不考虑直线绘制（freePath直线不偏移，不添加控制点，不以贝塞尔曲线生成）
        /// InsertPoint 贝塞尔曲线上内插点的个数
        public void SmoothFlowMap(List<Path> SubPaths, double MaxWidth, double MaxVolume, double MinVolume, int Type,int ODType, int OutType,int BeType,int InsertPoint)
        {
            PrDispalce.FlowMap.PublicUtil PU = new PublicUtil();
            SMap OutMap = new SMap();
            SMap OutEdgeMap = new SMap();

            foreach (Path Pa in SubPaths)
            {
                #region 计算宽度
                double CacheWidth = this.GetWidth(Pa, MaxWidth, 0.1, MaxVolume, MinVolume, Type);
                object PolylineSb = Sb.LineSymbolization(CacheWidth, 255, 0, 0, 0);
                #endregion

                #region 贝塞尔曲线绘制
                List<IPoint> ControlPoints = new List<IPoint>();

                #region 如果是直线（确定控制点）
                if (this.OnLine(Pa))
                {
                    #region 不考虑ODPoints
                    IPoint sPoint = new PointClass();
                    sPoint.X = (Grids[Pa.ePath[0]][0] + Grids[Pa.ePath[0]][2]) / 2;
                    sPoint.Y = (Grids[Pa.ePath[0]][1] + Grids[Pa.ePath[0]][3]) / 2;
                    #endregion

                    #region 考虑ODPoints
                    if (ODType == 1)
                    {
                        if (GridWithNode.Keys.Contains(Pa.ePath[0]))
                        {
                            sPoint.X = GridWithNode[Pa.ePath[0]].X;
                            sPoint.Y = GridWithNode[Pa.ePath[0]].Y; ;
                        }
                    }
                    #endregion

                    ControlPoints.Add(sPoint);

                    #region 不考虑ODPoints
                    IPoint ePoint = new PointClass();
                    ePoint.X = (Grids[Pa.ePath[Pa.ePath.Count-1]][0] + Grids[Pa.ePath[Pa.ePath.Count-1]][2]) / 2;
                    ePoint.Y = (Grids[Pa.ePath[Pa.ePath.Count-1]][1] + Grids[Pa.ePath[Pa.ePath.Count-1]][3]) / 2;
                    #endregion

                    #region 考虑ODPoints
                    if (ODType == 1)
                    {
                        if (GridWithNode.Keys.Contains(Pa.ePath[Pa.ePath.Count - 1]))
                        {
                            ePoint.X = GridWithNode[Pa.ePath[Pa.ePath.Count - 1]].X;
                            ePoint.Y = GridWithNode[Pa.ePath[Pa.ePath.Count - 1]].Y;
                        }
                    }
                    ControlPoints.Add(ePoint);
                    #endregion
                }
                #endregion

                #region 如果是非直线（确定控制点）
                else
                {
                    for (int j = 0; j < Pa.ePath.Count - 1; j++)
                    {
                        #region 不考虑ODPoints
                        IPoint sPoint = new PointClass();
                        sPoint.X = (Grids[Pa.ePath[j]][0] + Grids[Pa.ePath[j]][2]) / 2;
                        sPoint.Y = (Grids[Pa.ePath[j]][1] + Grids[Pa.ePath[j]][3]) / 2;
                        #endregion

                        #region 考虑ODPoints
                        if (ODType == 1)
                        {
                            if (GridWithNode.Keys.Contains(Pa.ePath[j]))
                            {
                                sPoint.X = GridWithNode[Pa.ePath[j]].X;
                                sPoint.Y = GridWithNode[Pa.ePath[j]].Y; ;
                            }
                        }
                        #endregion

                        ControlPoints.Add(sPoint);

                        if (j == Pa.ePath.Count - 2)
                        {
                            #region 不考虑ODPoints
                            IPoint ePoint = new PointClass();
                            ePoint.X = (Grids[Pa.ePath[j + 1]][0] + Grids[Pa.ePath[j + 1]][2]) / 2;
                            ePoint.Y = (Grids[Pa.ePath[j + 1]][1] + Grids[Pa.ePath[j + 1]][3]) / 2;
                            #endregion

                            #region 考虑ODPoints
                            if (ODType == 1)
                            {
                                if (GridWithNode.Keys.Contains(Pa.ePath[j + 1]))
                                {
                                    ePoint.X = GridWithNode[Pa.ePath[j + 1]].X;
                                    ePoint.Y = GridWithNode[Pa.ePath[j + 1]].Y;

                                }
                            }
                            ControlPoints.Add(ePoint);
                            #endregion
                        }
                    }
                }
                #endregion

                if (ControlPoints.Count > 0)
                {
                    BezierCurve BC = new BezierCurve(ControlPoints);//直接把控制点赋值给贝塞尔曲线了

                    #region 考虑直线绘制
                    if (BeType == 0)
                    {
                        #region 若FlowInPath=0，且与FlowOutPath不是直线
                        bool OnLineLabel = false;//不共线
                        if (ControlPoints.Count == 2)
                        {
                            for (int i = 0; i < Pa.FlowOutPath.Count; i++)
                            {
                                if (Pa.FlowOutPath[i].ePath.Count > 1) //判断该点不是起源点！
                                {
                                    IPoint CachePoint = new PointClass();

                                    CachePoint.X = (Grids[Pa.FlowOutPath[i].ePath[Pa.FlowOutPath[i].ePath.Count - 2]][0] + Grids[Pa.FlowOutPath[i].ePath[Pa.FlowOutPath[i].ePath.Count - 2]][2]) / 2;
                                    CachePoint.Y = (Grids[Pa.FlowOutPath[i].ePath[Pa.FlowOutPath[i].ePath.Count - 2]][1] + Grids[Pa.FlowOutPath[i].ePath[Pa.FlowOutPath[i].ePath.Count - 2]][3]) / 2;
                                    //CachePoint.X = (Grids[Pa.FlowOutPath[i].ePath[1]][0] + Grids[Pa.FlowOutPath[i].ePath[1]][2]) / 2;
                                    //CachePoint.Y = (Grids[Pa.FlowOutPath[i].ePath[1]][1] + Grids[Pa.FlowOutPath[i].ePath[1]][3]) / 2;

                                    double Angle = PU.GetAngle(ControlPoints[0], CachePoint, ControlPoints[1]);

                                    if (Math.Abs(Angle - Math.PI) < 0.001)
                                    {
                                        OnLineLabel = true;//共线
                                    }
                                }
                            }
                        }
                        #endregion

                        #region 贝塞尔曲线生成
                        if (Pa.FlowInPath.Count == 0 && !OnLineLabel) 
                        //if (Pa.FlowInPath.Count == 0)
                        {
                            #region 计算角度
                            FlowMapUtil FMU = new FlowMapUtil();
                            double Angle = 0;
                            if (Pa.FlowOutPath.Count > 0)
                            {
                                Angle = FMU.AnglePath(Pa.ePath, Pa.FlowOutPath[0], Grids);//计算角度
                            }
                            #endregion 

                            #region 流入的角度是非直线
                            if (Angle <= 17 * Math.PI / 18) 
                            {
                                ///依据位置确定控制点的位置
                                if (ControlPoints[1].X >= ControlPoints[0].X && ControlPoints[1].Y >= ControlPoints[0].Y)
                                {
                                    BC.CurveNGenerate(InsertPoint, 0.1, 0);
                                }

                                else if (ControlPoints[1].X >= ControlPoints[0].X && ControlPoints[1].Y < ControlPoints[0].Y)
                                {
                                    BC.CurveNGenerate(InsertPoint, 0.1, 1);
                                }

                                else if (ControlPoints[1].X < ControlPoints[0].X && ControlPoints[1].Y > ControlPoints[0].Y)
                                {
                                    BC.CurveNGenerate(InsertPoint, 0.1, 2);
                                }

                                else if (ControlPoints[1].X < ControlPoints[0].X && ControlPoints[1].Y < ControlPoints[0].Y)
                                {
                                    BC.CurveNGenerate(InsertPoint, 0.1, 3);
                                }
                            }
                            #endregion

                            #region 流入的角度是直线
                            else 
                            {
                                BC.CurveNGenerate(InsertPoint);
                            }
                            //BC.CurveNGenerate(InsertPoint, 0.1);
                            #endregion
                        }

                        else 
                        {
                            BC.CurveNGenerate(InsertPoint);
                        }
                        #endregion
                    }
                    #endregion

                    #region 不考虑直线绘制
                    else if (BeType == 1)
                    {
                        BC.CurveNGenerate(InsertPoint);
                    }
                    #endregion

                    #region 贝塞尔曲线绘制
                    List<TriNode> LinePoints = this.BCDraw(Pa, BC, OutType, MaxWidth, 0.1, MaxVolume, MinVolume, Type);//贝塞尔曲线绘制

                    #region ///函数代替
                    //List<TriNode> LinePoints = new List<TriNode>();//输出用
                    //for (int i = 0; i < BC.CurvePoint.Count - 1; i++)
                    //{
                    //    IPolyline iLine = new PolylineClass();
                    //    iLine.FromPoint = BC.CurvePoint[i];
                    //    iLine.ToPoint = BC.CurvePoint[i + 1];

                    //    #region 输出需要
                    //    if (OutType == 1)
                    //    {
                    //        TriNode pNode = new TriNode(BC.CurvePoint[i].X, BC.CurvePoint[i].Y);//输出用
                    //        LinePoints.Add(pNode);

                    //        if (i == BC.CurvePoint.Count - 2)//输出用
                    //        {
                    //            TriNode nNode = new TriNode(BC.CurvePoint[i + 1].X, BC.CurvePoint[i + 1].Y);
                    //            LinePoints.Add(nNode);
                    //        }
                    //    }
                    //    #endregion

                    //    pMapControl.DrawShape(iLine, ref PolylineSb);
                    //}
                    #endregion

                    #region 需要输出
                    if (OutType == 1)
                    {
                        PolylineObject CacheLine = new PolylineObject(LinePoints);
                        CacheLine.Volume = Pa.Volume;
                        CacheLine.SylWidth = CacheWidth;
                        OutMap.PolylineList.Add(CacheLine);

                        if (Pa.FlowInPath.Count == 0)
                        {
                            OutEdgeMap.PolylineList.Add(CacheLine);
                        }
                    }
                    #endregion

                    #endregion
                }
                #endregion
            }

            #region 需要输出
            if (OutType == 1 && OutPath != null)
            {
                OutMap.WriteResult2Shp(OutPath, pMapControl.Map.SpatialReference, 1);
                OutEdgeMap.WriteResult2Shp(OutPath, pMapControl.Map.SpatialReference, 3);
            }
            #endregion
        }

        /// <summary>
        /// 宽度按照给定参数变化绘制SmoothFlowMap(20220829 TVCG 修改稿)
        /// </summary>
        /// <param name="SubPaths">FlowMap</param>
        /// <param name="MaxWidth">最大宽度</param>
        /// <param name="MaxVolume">最大流量</param>
        /// <param name="MinVolume">最小流量</param>
        /// <param name="Type">Type=0宽度线性变化；=1宽度三角函数变化;Type=2宽度线性变化；</param>
        /// <param name="ODType">是否考虑ODPoint Type=0不考虑；Type=1考虑</param>
        /// OutType是否输出 =0；不输出 =1输出
        /// BeType =0 考虑直线绘制；=1不考虑直线绘制
        /// InsertPoint 贝塞尔曲线上内插点的个数
        /// Shift=0，考虑线宽度的偏移；shift=1 不进行偏移
        /// <param name="AngleThr">直线汇入的角度阈值——通过格网判断</param>
        /// FreeCount=false 不考虑是否是Free边；=True 考虑是否是Free边
        /// OnLineIn=false 不考虑是否直线汇入；=true 考虑是直线汇入
        /// GeoPrj=1 表示需要做投影变换;0表示不需要做投影变换（该参数不可靠）
        public void SmoothFlowMap_2(List<Path> SubPaths, double MaxWidth, double MinWidth, double MaxVolume, double MinVolume, int Type, int ODType, double Rate, double OnLineDis,int OutType, int InsertPoint, int Shift, double AngleThr,bool FreeCount,bool OnLineIn,int GeoPrj)
        {
            PrDispalce.FlowMap.PublicUtil PU = new PublicUtil();
            FlowMapUtil FMU = new FlowMapUtil();
            SMap OutMap = new SMap();
            SMap OutEdgeMap = new SMap();

            #region 贝塞尔曲线生成
            int LabelCount = 0;
            foreach (Path Pa in SubPaths)
            {
                LabelCount++;
                List<IPoint> ControlPoints = new List<IPoint>();

                #region 获取控制点
                if (Shift == 0)//偏移
                {
                    ControlPoints = this.GetShiftControlPoints_2(Pa, Grids, GridWithNode, AngleThr, ODType, Rate, OnLineDis, MaxWidth, MinWidth, MaxVolume, MinVolume, Type, FreeCount, OnLineIn, GeoPrj);
                }
                if (Shift == 1)//不偏移
                {
                    ControlPoints = this.GetControlPoints(Pa, Grids, GridWithNode, AngleThr, ODType, Rate,OnLineDis, FreeCount, OnLineIn);
                }
                #endregion

                BezierCurve BC = new BezierCurve(ControlPoints);//直接把控制点赋值给贝塞尔曲线了
                BC.CurveNGenerate(InsertPoint);//贝塞尔曲线生成
                List<TriNode> LinePoints = this.BCDraw(Pa, BC, OutType, MaxWidth, MinWidth, MaxVolume, MinVolume, Type);//贝塞尔曲线绘制

                #region 需要输出
                if (OutType == 1)
                {
                    PolylineObject CacheLine = new PolylineObject(LinePoints);
                    CacheLine.Volume = Pa.Volume;
                    CacheLine.shift = Pa.shift;
                    CacheLine.LOrR = Pa.LOrR;
                    CacheLine.GetLength();//计算长度
                    CacheLine.SmoothValue = PU.GetSmoothValue(CacheLine);
                    CacheLine.FlowIn = Pa.FlowInPath.Count;
                    CacheLine.FlowOut = Pa.FlowOutPath.Count;

                    if (Pa.FlowOutPath.Count > 0)//标识每条路径流出的路径编号
                    {
                        CacheLine.FlowOutId = SubPaths.IndexOf(Pa.FlowOutPath[0]);
                    }

                    double CacheWidth = this.GetWidth(Pa, MaxWidth, MinWidth, MaxVolume, MinVolume, Type);//符号宽度
                    CacheLine.SylWidth = CacheWidth;
                    OutMap.PolylineList.Add(CacheLine);
                }
                #endregion
            }
            #endregion

            #region 需要输出
            if (OutType == 1 && OutPath != null)
            {
                OutMap.WriteResult2Shp(OutPath, pMapControl.Map.SpatialReference, 1);
            }
            #endregion
        }

        /// <summary>
        /// 针对给定的Layout
        /// </summary>
        /// <param name="PLList"></param>
        /// <param name="AngleThr"></param>
        /// <param name="FreeCount"></param>
        /// <param name="OnLineIn"></param>
        /// <param name="Rate">贝塞尔曲线1偏移比例</param>
        /// <param name="Cur2DisRate">贝塞尔曲线2偏移比例</param>
        /// <param name="OutType"></param>
        /// <param name="InsertPoint"></param>
        public void LayoutSmoothMap(List<PolylineObject> PLList,double AngleThr,bool FreeCount,bool OnLineIn,double Rate,double Cur2DisRate,int OutType,int InsertPoint)
        {
            PrDispalce.FlowMap.PublicUtil PU = new PublicUtil();
            FlowMapUtil FMU = new FlowMapUtil();
            SMap OutMap = new SMap();
            SMap OutEdgeMap = new SMap();

            #region 贝塞尔曲线生成
            int LabelCount = 0;
            foreach (PolylineObject PL in PLList)
            {
                LabelCount++;
                List<IPoint> ControlPoints = this.GetShiftControlPoints_3(PL, PLList, AngleThr, FreeCount, OnLineIn, Rate, Cur2DisRate);

                BezierCurve BC = new BezierCurve(ControlPoints);//直接把控制点赋值给贝塞尔曲线了
                BC.CurveNGenerate(InsertPoint);//贝塞尔曲线生成
                List<TriNode> LinePoints = this.BCDraw(BC, OutType, PL.SylWidth);//贝塞尔曲线绘制

                #region 需要输出
                if (OutType == 1)
                {
                    PolylineObject CacheLine = new PolylineObject(LinePoints);
                    CacheLine.Volume = PL.Volume;
                    CacheLine.shift = PL.shift;
                    CacheLine.LOrR = PL.LOrR;
                    CacheLine.GetLength();//计算长度
                    CacheLine.SmoothValue = PU.GetSmoothValue(CacheLine);
                    CacheLine.FlowIn = PL.FlowIn;
                    CacheLine.FlowOut = PL.FlowOut;
                    CacheLine.SylWidth = PL.SylWidth;
                    CacheLine.Volume = PL.Volume;
                    CacheLine.ShiftDis = PL.ShiftDis;
                    OutMap.PolylineList.Add(CacheLine);
                }
                #endregion
            }
            #endregion

            #region 需要输出
            if (OutType == 1 && OutPath != null)
            {
                OutMap.WriteResult2Shp(OutPath, pMapControl.Map.SpatialReference, 1);
            }
            #endregion
        }      

        /// <summary>
        /// 对给定的贝塞尔曲线进行绘制
        /// </summary>
        /// <param name="Pa">当前路径</param>
        /// <param name="BC">路径的贝塞尔曲线</param>
        /// <param name="OutType">OutType是否输出 =0；不输出 =1输出</param>
        /// <param name="MaxWidth"></param>最大宽度
        /// <param name="MinWidth"></param>最小宽度
        /// <param name="MaxVolume"></param>最大流量
        /// <param name="MinVolume"></param>最小流量
        /// <param name="Type"></param>Type=0宽度三角函数变化；Type=1宽度近似线性变化；Type=2宽度线性变化；
        /// <returns></returns> 返回贝塞尔曲线上的节点
        public List<TriNode> BCDraw(Path Pa, BezierCurve BC, int OutType, double MaxWidth, double MinWidth, double MaxVolume, double MinVolume, int Type)
        {
            double CacheWidth = this.GetWidth(Pa, MaxWidth, MinWidth, MaxVolume, MinVolume, Type);//符号
            object PolylineSb = Sb.LineSymbolization(CacheWidth, 255, 0, 0, 0);

            List<TriNode> LinePoints = new List<TriNode>();//输出用
            for (int i = 0; i < BC.CurvePoint.Count - 1; i++)
            {
                IPolyline iLine = new PolylineClass();
                iLine.FromPoint = BC.CurvePoint[i];
                iLine.ToPoint = BC.CurvePoint[i + 1];

                #region 输出需要
                if (OutType == 1)
                {
                    TriNode pNode = new TriNode(BC.CurvePoint[i].X, BC.CurvePoint[i].Y);//输出用
                    LinePoints.Add(pNode);

                    if (i == BC.CurvePoint.Count - 2)//输出用
                    {
                        TriNode nNode = new TriNode(BC.CurvePoint[i + 1].X, BC.CurvePoint[i + 1].Y);
                        LinePoints.Add(nNode);
                    }
                }
                #endregion

                pMapControl.DrawShape(iLine, ref PolylineSb);
            }

            return LinePoints;
        }

        /// <summary>
        /// 对给定的贝塞尔曲线进行绘制
        /// </summary>
        /// <param name="Pa">当前路径</param>
        /// <param name="BC">路径的贝塞尔曲线</param>
        /// <param name="OutType">OutType是否输出 =0；不输出 =1输出</param>
        /// <returns></returns> 返回贝塞尔曲线上的节点
        public List<TriNode> BCDraw(BezierCurve BC, int OutType, double CacheWidth)
        {
            object PolylineSb = Sb.LineSymbolization(CacheWidth, 255, 0, 0, 0);

            List<TriNode> LinePoints = new List<TriNode>();//输出用
            for (int i = 0; i < BC.CurvePoint.Count - 1; i++)
            {
                IPolyline iLine = new PolylineClass();
                iLine.FromPoint = BC.CurvePoint[i];
                iLine.ToPoint = BC.CurvePoint[i + 1];

                #region 输出需要
                if (OutType == 1)
                {
                    TriNode pNode = new TriNode(BC.CurvePoint[i].X, BC.CurvePoint[i].Y);//输出用
                    LinePoints.Add(pNode);

                    if (i == BC.CurvePoint.Count - 2)//输出用
                    {
                        TriNode nNode = new TriNode(BC.CurvePoint[i + 1].X, BC.CurvePoint[i + 1].Y);
                        LinePoints.Add(nNode);
                    }
                }
                #endregion

                pMapControl.DrawShape(iLine, ref PolylineSb);
            }

            return LinePoints;
        }

        /// <summary>
        /// 判断给定的路径是否是直线
        /// </summary>
        /// <param name="CachePath"></param>
        /// <returns></returns>
        /// true表示Online；false表示不Online
        public bool OnLine(Path CachePath)
        {
            if (CachePath.ePath.Count > 2)
            {
                for (int i = 0; i < CachePath.ePath.Count - 2; i++)
                {
                    int add11 = CachePath.ePath[i].Item1 - CachePath.ePath[i + 1].Item1;
                    int add12 = CachePath.ePath[i].Item2 - CachePath.ePath[i + 1].Item2;

                    int add21 = CachePath.ePath[i + 1].Item1 - CachePath.ePath[i + 2].Item1;
                    int add22 = CachePath.ePath[i + 1].Item2 - CachePath.ePath[i + 2].Item2;

                    if (add11 !=add21 || add12 != add22)
                    {
                        return false;
                    }
                }

                return true;
            }

            else
            {
                return true;
            }
        }

        /// <summary>
        /// 获取控制点（1.直线汇入不添加控制点；2.非直线汇入添加控制点，控制点添加方法：以当前一段两点距离的一定比例在其流入路径方向上的某点作为控制点）需要插入控制点
        /// 未考虑偏移
        /// </summary>
        /// <param name="Pa">当前路径</param>
        /// <param name="Grids"></param>
        /// <param name="GridWithNode"></param>
        /// <param name="AngleThr">直线汇入的角度阈值——通过格网判断</param>
        /// <param name="ODType">是否考虑格网中的点</param>
        /// <param name="Rate">贝塞尔曲线偏移距离比率</param>
        /// FreeCount=false 不考虑是否是Free边；=True 考虑是否是Free边
        /// OnLineIn=false 不考虑是否直线汇入；=true 考虑是直线汇入【考虑非自由边是否平滑汇入主流】
        /// <returns></returns>
        public List<IPoint> GetControlPoints(Path Pa, Dictionary<Tuple<int, int>, List<double>> Grids, Dictionary<Tuple<int, int>, IPoint> GridWithNode,double AngleThr,double ODType,double Rate,double OnLineDis,bool FreeCount,bool OnLineIn)
        {
            List<IPoint> ControlPoints = new List<IPoint>();
            FlowMapUtil FMU = new FlowMapUtil();
            bool FlowInOnLine = FMU.FlowPathOnLine_2(Pa, Grids, AngleThr);//路径是否直线流入主流
            //int LeftOrRigth = FMU.FlowPathLeftOrRight(Pa, Grids);//判断支流是在主流的左侧还是右侧
            
            ControlPoints = this.GetTurningPoints(Pa, Grids, GridWithNode, ODType);

            #region 考虑是否是自由边和不考虑非自由边中的非直线汇入（只对自由边添加控制点）【直线汇入添加对应控制点绘制贝塞尔曲线也是直线！】
            if (FreeCount && !OnLineIn)
            {
                #region 添加控制点 如果是自由边
                if (Pa.FlowInPath.Count == 0)
                {
                    if (Pa.FlowOutPath.Count > 0)
                    {
                        if (Pa.FlowOutPath[0].ePath.Count > 1)//判断该点不是起源点！
                        {
                            IPoint InsertControlPoint = this.GetInsertControlPoint(Pa, Grids, GridWithNode, ODType, Rate);
                            ControlPoints.Insert(1, InsertControlPoint);//插入该控制点
                        }
                    }
                }
                #endregion
            }
            #endregion

            #region 考虑是否是自由边；同时，不考虑非自由边中的非直线汇入
            else if (FreeCount && OnLineIn)
            {
                #region 如果是自由边，则添加控制点 
                if (Pa.FlowInPath.Count == 0)
                {
                    if (Pa.FlowOutPath.Count > 0)
                    {
                        if (Pa.FlowOutPath[0].ePath.Count > 1)//判断该点不是起源点！
                        {
                            IPoint InsertControlPoint = this.GetInsertControlPoint(Pa, Grids, GridWithNode, ODType, Rate);
                            ControlPoints.Insert(1, InsertControlPoint);//插入该控制点
                        }
                    }
                }
                #endregion

                #region 如果是非自由边且是非直线汇入，则添加控制点
                else
                {
                    if (!FlowInOnLine) //不是直线汇入
                    {
                        if (Pa.FlowOutPath.Count > 0)
                        {
                            if (Pa.FlowOutPath[0].ePath.Count > 1)//判断该点不是起源点！
                            {
                                List<IPoint> InsertControlPoints = this.GetInsertControlPoint_2(Pa, Grids, GridWithNode, ODType, OnLineDis);
                                ControlPoints.Insert(1, InsertControlPoints[0]);//插入该控制点
                                ControlPoints.Insert(2, InsertControlPoints[1]);//插入该控制点
                            }
                        }
                    }
                }
                #endregion
            }
            #endregion

            #region 对于任意的边都添加控制点【以简单贝塞尔曲线来添加控制点！】
            else
            {
                #region 考虑是否是直线汇入
                if (OnLineIn && FlowInOnLine)
                {
                    if (Pa.FlowOutPath.Count > 0)
                    {
                        if (Pa.FlowOutPath[0].ePath.Count > 1)//判断该点不是起源点！
                        {
                            IPoint InsertControlPoint = this.GetInsertControlPoint(Pa, Grids, GridWithNode, ODType, Rate);
                            ControlPoints.Insert(1, InsertControlPoint);//插入该控制点
                        }
                    }
                }
                #endregion
            }
            #endregion
       
            return ControlPoints;
        }

        /// <summary>
        /// 获得按宽度偏移后的控制点！（先获取控制点，再偏移——结果偏差会很大！）
        /// </summary>
        /// <param name="Pa">当前路径</param>
        /// <param name="Grids"></param>
        /// <param name="GridWithNode"></param>
        /// <param name="AngleThr">直线汇入的角度阈值——通过格网判断</param>
        /// <param name="ODType">是否考虑格网中的点</param>
        /// <param name="Rate">贝塞尔曲线偏移距离比率</param>
        /// MinWidth最小宽度
        /// MaxVolume最大流量
        /// MinVolume最小流量
        /// Type=0宽度三角函数变化；Type=1宽度近似线性变化；Type=2宽度线性变化；
        /// FreeCount=false 不考虑是否是Free边；=True 考虑是否是Free边
        /// OnLineIn=false 不考虑是否直线汇入；=true 考虑是直线汇入
        /// GeoPrj=1表示需要做坐标转换（该参数不可靠）
        /// <returns></returns>
        public List<IPoint> GetShiftControlPoints(Path Pa, Dictionary<Tuple<int, int>, List<double>> Grids, Dictionary<Tuple<int, int>, IPoint> GridWithNode, double AngleThr, double ODType, double Rate, double OnLineDis, double MaxWidth, double MinWidth, double MaxVolume, double MinVolume, int Type,bool FreeCount,bool OnLineIn,int GeoPrj)
        {
            List<IPoint> ControlPoints = this.GetControlPoints(Pa, Grids, GridWithNode, AngleThr, ODType, Rate, OnLineDis, FreeCount, OnLineIn);//这里获取的控制点既考虑直线汇入的控制点，也考虑非直线汇入的控制点
            FlowMapUtil FMU = new FlowMapUtil();
            PublicUtil PU = new PublicUtil();

            if (Pa.FlowOutPath.Count > 0)
            {
                #region 若汇入路径不是一条；汇入路径如果只有1条则不变换
                if (Pa.FlowOutPath[0].FlowInPath.Count > 1)
                {
                    if (Pa.FlowOutPath[0].ePath.Count > 1) //连接的不是起始点
                    {                      
                        #region 获取需要偏移的两点【流入路径方向 sPoint和ePoint（流入路径的最后两个点）】
                        #region 不考虑ODPoints
                        IPoint sPoint = new PointClass();
                        sPoint.X = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][0] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][2]) / 2;
                        sPoint.Y = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][1] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][3]) / 2;
                        #endregion

                        #region 考虑ODPoints
                        if (ODType == 1)
                        {
                            if (GridWithNode.Keys.Contains(Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]))
                            {
                                sPoint.X = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]].X;
                                sPoint.Y = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]].Y; ;
                            }
                        }
                        #endregion

                        #region 不考虑ODPoints
                        IPoint ePoint = new PointClass();
                        ePoint.X = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][0] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][2]) / 2;
                        ePoint.Y = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][1] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][3]) / 2;
                        #endregion

                        #region 考虑ODPoints
                        if (ODType == 1)
                        {
                            if (GridWithNode.Keys.Contains(Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]))
                            {
                                ePoint.X = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]].X;
                                ePoint.Y = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]].Y;
                            }
                        }
                        #endregion
                        #endregion

                        #region 计算偏移方向或偏移距离
                        int ShiftOri = 2;//2不偏移；1向左偏移；3向右偏移(在此)
                        double ShiftDis = FMU.FlowPathShiftDis(Pa, Grids, out ShiftOri, MaxWidth, MinWidth, MaxVolume, MinVolume, Type, GeoPrj);
                        Pa.shift = ShiftOri;
                        #endregion

                        #region 对给定的点进行偏移
                        PU.GetShiftPoint(sPoint, ePoint, ControlPoints[0], ShiftDis, ShiftOri);//第一个点的偏移（偏移直接反馈至ControlPoints[0]）
                        //PU.GetShiftPoint(sPoint, ePoint, ControlPoints[1], ShiftDis, ShiftOri);//第二个点的偏移（偏移直接反馈至ControlPoints[0]）
                        #endregion 
                    }
                }
                #endregion 
            }

            return ControlPoints;
        }

        /// <summary>
        /// 获得按宽度偏移后的控制点！(先偏移，后获取控制点)
        /// </summary>
        /// <param name="Pa">当前路径</param>
        /// <param name="Grids"></param>
        /// <param name="GridWithNode"></param>
        /// <param name="AngleThr">直线汇入的角度阈值——通过格网判断</param>
        /// <param name="ODType">是否考虑格网中的点</param>
        /// <param name="Rate">贝塞尔曲线偏移距离比率</param>
        /// MinWidth最小宽度
        /// MaxVolume最大流量
        /// MinVolume最小流量
        /// Type=0宽度三角函数变化；Type=1宽度近似线性变化；Type=2宽度线性变化；
        /// FreeCount=false 不考虑是否是Free边；=True 考虑是否是Free边
        /// OnLineIn=false 不考虑是否直线汇入；=true 考虑是直线汇入
        /// GeoPrj=1 表示需要做坐标变换；=0表示不需要做坐标转换（该参数不可靠）
        /// <returns></returns>
        public List<IPoint> GetShiftControlPoints_2(Path Pa, Dictionary<Tuple<int, int>, List<double>> Grids, Dictionary<Tuple<int, int>, IPoint> GridWithNode, double AngleThr, double ODType, double Rate, double OnLineDis, double MaxWidth, double MinWidth, double MaxVolume, double MinVolume, int Type, bool FreeCount, bool OnLineIn,int GeoPrj)
        {
            List<IPoint> ControlPoints = new List<IPoint>();
            FlowMapUtil FMU = new FlowMapUtil();
            bool FlowInOnLine = FMU.FlowPathOnLine_2(Pa, Grids, AngleThr);//路径是否直线流入主流
            //int LeftOrRigth = FMU.FlowPathLeftOrRight(Pa, Grids);//判断支流是在主流的左侧还是右侧

            ControlPoints = this.GetTurningPoints(Pa, Grids, GridWithNode, ODType);

            #region 考虑是否是自由边和不考虑非自由边中的非直线汇入（只对自由边添加控制点）【直线汇入添加对应控制点绘制贝塞尔曲线也是直线！】
            if (FreeCount && !OnLineIn)
            {
                #region  如果是自由边且非直线汇入，添加控制点
                if (Pa.FlowInPath.Count == 0)
                {
                    if (Pa.FlowOutPath.Count > 0)
                    {
                        if (Pa.FlowOutPath[0].ePath.Count > 1)//判断该点不是起源点！
                        {
                            List<IPoint> InsertControlPoint = this.GetShiftInsertControlPoint(Pa, Grids, GridWithNode, ODType, Rate, MaxWidth, MinWidth, MaxVolume, MinVolume, Type,GeoPrj);
                            ControlPoints[0] = InsertControlPoint[0];//插入该控制点
                            ControlPoints.Insert(1, InsertControlPoint[1]);
                        }
                    }
                }
                #endregion
            }
            #endregion

            #region 考虑是否是自由边；同时，考虑非自由边中的非直线汇入
            else if (FreeCount && OnLineIn)
            {
                #region 如果是自由边，则添加控制点
                if (Pa.FlowInPath.Count == 0)
                {
                    if (Pa.FlowOutPath.Count > 0)
                    {
                        if (Pa.FlowOutPath[0].ePath.Count > 1)//判断该点不是起源点！
                        {
                            List<IPoint> InsertControlPoint = this.GetShiftInsertControlPoint(Pa, Grids, GridWithNode, ODType, Rate, MaxWidth, MinWidth, MaxVolume, MinVolume, Type,GeoPrj);
                            ControlPoints[0] = InsertControlPoint[0];//插入该控制点
                            ControlPoints.Insert(1, InsertControlPoint[1]);
                        }
                    }
                }
                #endregion

                #region 如果是非自由边且是非直线汇入，则添加控制点
                else
                {
                    if (!FlowInOnLine) //不是直线汇入
                    {
                        if (Pa.FlowOutPath.Count > 0)
                        {
                            if (Pa.FlowOutPath[0].ePath.Count > 1)//判断该点不是起源点！
                            {
                                List<IPoint> InsertControlPoints = this.GetShiftInsertControlPoint_2(Pa, Grids, GridWithNode, ODType, OnLineDis, MaxWidth, MinWidth, MaxVolume, MinVolume, Type, GeoPrj);
                                ControlPoints[0] = InsertControlPoints[0];
                                ControlPoints.Insert(1, InsertControlPoints[1]);//插入该控制点
                                ControlPoints.Insert(2, InsertControlPoints[2]);//插入该控制点
                            }
                        }
                    }
                }
                #endregion
            }
            #endregion

            #region 对于任意的边都添加控制点【以简单贝塞尔曲线来添加控制点！】
            else
            {
                #region 考虑是否是直线汇入
                if (OnLineIn && FlowInOnLine)
                {
                    if (Pa.FlowOutPath.Count > 0)
                    {
                        if (Pa.FlowOutPath[0].ePath.Count > 1)//判断该点不是起源点！
                        {
                            List<IPoint> InsertControlPoint = this.GetShiftInsertControlPoint(Pa, Grids, GridWithNode, ODType, Rate, MaxWidth, MinWidth, MaxVolume, MinVolume, Type, GeoPrj);
                            ControlPoints[0] = InsertControlPoint[0];//插入该控制点
                            ControlPoints.Insert(1, InsertControlPoint[1]);
                        }
                    }
                }
                #endregion
            }
            #endregion

            return ControlPoints;
        }

        
        /// <summary>
        /// 获得按宽度偏移后的控制点！
        /// </summary>
        /// <param name="TarPL">给定路径</param>
        /// <param name="PLList">路径List</param>
        /// <param name="AngleThr">直线汇入的角度阈值</param>
        /// <param name="FreeCount">FreeCount=false 不考虑是否是Free边；=True 考虑是否是Free边</param>
        /// <param name="OnLineIn">是否考虑共线问题 OnLineIn=false 不考虑是否直线汇入；=true 考虑是直线汇入</param>
        /// <param name="Rate">贝塞尔曲线1控制点偏移比例</param>
        /// <param name="Cur2DisRate">贝塞尔曲线2控制点偏移距离比例</param>
        /// <returns></returns>
        public List<IPoint> GetShiftControlPoints_3(PolylineObject TarPL,List<PolylineObject> PLList,double AngleThr,bool FreeCount,bool OnLineIn,double Rate,double Cur2DisRate)
        {
            List<IPoint> ControlPoints = new List<IPoint>();
            FlowMapUtil FMU = new FlowMapUtil();
            PublicUtil PU=new PublicUtil();
            ControlPoints = PU.GetPoints(TarPL);//获得转折点
            bool FlowInOnLine = FMU.FlowPathOnLine_2(TarPL,PLList, AngleThr);//路径是否直线流入主流

            #region 考虑是否是自由边和不考虑非自由边中的非直线汇入（只对自由边添加控制点）【直线汇入添加对应控制点绘制贝塞尔曲线也是直线！】
            if (FreeCount && !OnLineIn)
            {
                #region  如果是自由边且非直线汇入，添加控制点
                if (TarPL.FlowIn == 0 && !FlowInOnLine)//是自由边，且非直线汇入
                {
                    if (TarPL.FlowOut > 0)//不是起点
                    {
                        if (PLList[TarPL.FlowOutId].FlowOut > 0)//汇入的不是起点
                        {
                            List<IPoint> FlowOutPathPoints = PU.GetPoints(PLList[TarPL.FlowOutId]);
                            List<IPoint> InsertControlPoint = this.GetShiftInsertControlPoint(ControlPoints, FlowOutPathPoints[FlowOutPathPoints.Count - 2], FlowOutPathPoints[FlowOutPathPoints.Count - 1], TarPL.ShiftDis, Rate);//偏移距离已提前计算！
                            ControlPoints[0] = InsertControlPoint[0];//插入该控制点
                            ControlPoints.Insert(1, InsertControlPoint[1]);
                        }
                    }
                }
                #endregion
            }
            #endregion

            #region 考虑是否是自由边；同时，不考虑非自由边中的非直线汇入
            else if (FreeCount && OnLineIn)
            {
                #region 如果是自由边，则添加控制点
                if (TarPL.FlowIn == 0)//判断是否是自由边
                {
                    if (TarPL.FlowOut > 0)//不是起点
                    {
                        if (PLList[TarPL.FlowOutId].FlowOut > 0)//汇入的不是起点
                        {
                            List<IPoint> FlowOutPathPoints = PU.GetPoints(PLList[TarPL.FlowOutId]);
                            List<IPoint> InsertControlPoint = this.GetShiftInsertControlPoint(ControlPoints, FlowOutPathPoints[FlowOutPathPoints.Count - 2], FlowOutPathPoints[FlowOutPathPoints.Count - 1], TarPL.ShiftDis, Rate);//偏移距离已提前计算！
                            ControlPoints[0] = InsertControlPoint[0];//插入该控制点
                            ControlPoints.Insert(1, InsertControlPoint[1]);
                        }
                    }
                }
                #endregion

                #region 如果是非自由边且是非直线汇入，则添加控制点
                else
                {
                    if (!FlowInOnLine) //不是直线汇入
                    {
                        if (TarPL.FlowOut > 0)//不是起点
                        {
                            if (PLList[TarPL.FlowOutId].FlowOut > 0)//汇入的不是起点
                            {
                                List<IPoint> FlowOutPathPoints = PU.GetPoints(PLList[TarPL.FlowOutId]);
                                List<IPoint> InsertControlPoints = this.GetShiftInsertControlPoint_2(ControlPoints, FlowOutPathPoints[FlowOutPathPoints.Count - 2], FlowOutPathPoints[FlowOutPathPoints.Count - 1], TarPL.ShiftDis, Cur2DisRate);//偏移距离已提前计算！
                                ControlPoints[0] = InsertControlPoints[0];
                                ControlPoints.Insert(1, InsertControlPoints[1]);//插入该控制点
                                ControlPoints.Insert(2, InsertControlPoints[2]);//插入该控制点
                            }
                        }
                    }
                }
                #endregion
            }
            #endregion

            #region 对于任意的边都添加控制点【以简单贝塞尔曲线来添加控制点！】
            else
            {
                #region 考虑是否是直线汇入
                if (OnLineIn && FlowInOnLine)
                {
                    if (TarPL.FlowOut > 0)//不是起点
                    {
                        if (PLList[TarPL.FlowOutId].FlowOut > 0)//汇入的不是起点
                        {
                            List<IPoint> FlowOutPathPoints = PU.GetPoints(PLList[TarPL.FlowOutId]);
                            List<IPoint> InsertControlPoint = this.GetShiftInsertControlPoint(ControlPoints, FlowOutPathPoints[FlowOutPathPoints.Count - 2], FlowOutPathPoints[FlowOutPathPoints.Count - 1], TarPL.ShiftDis, Rate);
                            ControlPoints[0] = InsertControlPoint[0];//插入该控制点
                            ControlPoints.Insert(1, InsertControlPoint[1]);
                        }
                    }
                }
                #endregion
            }
            #endregion

            return ControlPoints;
        }

        /// <summary>
        /// 获得一条路径的转折点
        /// </summary>
        /// <param name="Pa">给定路径</param>
        /// <param name="Grids">Grid编号</param>
        /// <param name="GridWithNode">包含Node的Grid编号</param>
        /// <param name="ODType">是否考虑Grid中点的位置 ODPoints</param>
        /// <returns></returns>
        public List<IPoint> GetTurningPoints(Path Pa, Dictionary<Tuple<int, int>, List<double>> Grids, Dictionary<Tuple<int, int>, IPoint> GridWithNode, double ODType)
        {
            List<IPoint> TurningPoints = new List<IPoint>();
            bool OnLine = this.OnLine(Pa);//路径是否是直线(在本算法的隐含意思就是它包括几个转折点，若是直线，无转折点；若不是直线，存在转折点!!)

            #region 直线连接的Path
            if (OnLine)
            {
                #region 不考虑ODPoints
                IPoint sPoint = new PointClass();
                sPoint.X = (Grids[Pa.ePath[0]][0] + Grids[Pa.ePath[0]][2]) / 2;
                sPoint.Y = (Grids[Pa.ePath[0]][1] + Grids[Pa.ePath[0]][3]) / 2;
                #endregion

                #region 考虑ODPoints
                if (ODType == 1)
                {
                    if (GridWithNode.Keys.Contains(Pa.ePath[0]))
                    {
                        sPoint.X = GridWithNode[Pa.ePath[0]].X;
                        sPoint.Y = GridWithNode[Pa.ePath[0]].Y; ;
                    }
                }
                #endregion

                TurningPoints.Add(sPoint);

                #region 不考虑ODPoints
                IPoint ePoint = new PointClass();
                ePoint.X = (Grids[Pa.ePath[Pa.ePath.Count - 1]][0] + Grids[Pa.ePath[Pa.ePath.Count - 1]][2]) / 2;
                ePoint.Y = (Grids[Pa.ePath[Pa.ePath.Count - 1]][1] + Grids[Pa.ePath[Pa.ePath.Count - 1]][3]) / 2;
                #endregion

                #region 考虑ODPoints
                if (ODType == 1)
                {
                    if (GridWithNode.Keys.Contains(Pa.ePath[Pa.ePath.Count - 1]))
                    {
                        ePoint.X = GridWithNode[Pa.ePath[Pa.ePath.Count - 1]].X;
                        ePoint.Y = GridWithNode[Pa.ePath[Pa.ePath.Count - 1]].Y;
                    }
                }          
                #endregion

                TurningPoints.Add(ePoint);
            }
            #endregion

            #region 非直线连接的Path
            else
            {
                for (int j = 0; j < Pa.ePath.Count; j++)
                {
                    #region 第一个点
                    if (j == 0)
                    {
                        #region 不考虑ODPoints
                        IPoint sPoint = new PointClass();
                        sPoint.X = (Grids[Pa.ePath[j]][0] + Grids[Pa.ePath[j]][2]) / 2;
                        sPoint.Y = (Grids[Pa.ePath[j]][1] + Grids[Pa.ePath[j]][3]) / 2;
                        #endregion

                        #region 考虑ODPoints
                        if (ODType == 1)
                        {
                            if (GridWithNode.Keys.Contains(Pa.ePath[j]))
                            {
                                sPoint.X = GridWithNode[Pa.ePath[j]].X;
                                sPoint.Y = GridWithNode[Pa.ePath[j]].Y; ;
                            }
                        }
                        #endregion

                        TurningPoints.Add(sPoint);
                    }
                    #endregion

                    #region 最后一个点
                    else if (j == Pa.ePath.Count - 1)
                    {
                        #region 不考虑ODPoints
                        IPoint ePoint = new PointClass();
                        ePoint.X = (Grids[Pa.ePath[j]][0] + Grids[Pa.ePath[j]][2]) / 2;
                        ePoint.Y = (Grids[Pa.ePath[j]][1] + Grids[Pa.ePath[j]][3]) / 2;
                        #endregion

                        #region 考虑ODPoints
                        if (ODType == 1)
                        {
                            if (GridWithNode.Keys.Contains(Pa.ePath[j]))
                            {
                                ePoint.X = GridWithNode[Pa.ePath[j]].X;
                                ePoint.Y = GridWithNode[Pa.ePath[j]].Y;

                            }
                        }
                        TurningPoints.Add(ePoint);
                        #endregion
                    }
                    #endregion

                    #region 其它点（判断是否是转折点）只添加转折点
                    else
                    {
                        if(((Pa.ePath[j+1].Item1-Pa.ePath[j].Item1)!=(Pa.ePath[j].Item1-Pa.ePath[j-1].Item1))||
                            ((Pa.ePath[j+1].Item2-Pa.ePath[j].Item2)!=(Pa.ePath[j].Item2-Pa.ePath[j-1].Item2)))
                        {
                            #region 不考虑ODPoints
                            IPoint sPoint = new PointClass();
                            sPoint.X = (Grids[Pa.ePath[j]][0] + Grids[Pa.ePath[j]][2]) / 2;
                            sPoint.Y = (Grids[Pa.ePath[j]][1] + Grids[Pa.ePath[j]][3]) / 2;
                            #endregion

                            #region 考虑ODPoints
                            if (ODType == 1)
                            {
                                if (GridWithNode.Keys.Contains(Pa.ePath[j]))
                                {
                                    sPoint.X = GridWithNode[Pa.ePath[j]].X;
                                    sPoint.Y = GridWithNode[Pa.ePath[j]].Y; ;
                                }
                            }
                            #endregion

                            TurningPoints.Add(sPoint);
                        }
                        
                    }
                    #endregion
                }
            }
            #endregion

            return TurningPoints;
        }

        /// <summary>
        /// 获得给定线TriNodeList中的转折点
        /// </summary>
        /// <param name="NodeList"></param>
        /// <returns></returns>
        public List<TriNode> GetTurningPoints(List<TriNode> TriNodeList)
        {
            List<TriNode> TurningNodeList = new List<TriNode>();

            #region 节点数小于等于2
            if (TriNodeList.Count <= 2)
            {
                TurningNodeList = TriNodeList;
            }
            #endregion

            #region 节点数大于2
            else
            {
                for (int j = 0; j < TriNodeList.Count; j++)
                {
                    #region 第一个点
                    if (j == 0)
                    {
                        TurningNodeList.Add(TriNodeList[j]);
                    }
                    #endregion

                    #region 最后一个点
                    else if (j == TriNodeList.Count - 1)
                    {
                        TurningNodeList.Add(TriNodeList[j]);
                    }
                    #endregion

                    #region 其它点（判断是否是转折点）只添加转折点
                    else
                    {
                        if (((TriNodeList[j+1].Y-TriNodeList[j].Y)/(TriNodeList[j+1].X-TriNodeList[j].X))!=
                            ((TriNodeList[j].Y-TriNodeList[j-1].Y)/(TriNodeList[j].X-TriNodeList[j-1].X)))
                        {
                            TurningNodeList.Add(TriNodeList[j]);
                        }

                    }
                    #endregion
                }

            }
            #endregion

            return TurningNodeList;
        }

        /// <summary>
        /// 获得当前路径需嵌入的控制点（嵌入方式1-自由边控制点嵌入）
        /// </summary>
        /// <param name="Pa"></param>当前路径
        /// <param name="Grids"></param>Grid
        /// <param name="GridWithNode"></param>GridwithNodes
        /// <param name="ODType"></param>是否考虑ODPoints
        /// <param name="rate"></param> 控制点移动的距离
        /// <returns></returns>
        public IPoint GetInsertControlPoint(Path Pa, Dictionary<Tuple<int, int>, List<double>> Grids, Dictionary<Tuple<int, int>, IPoint> GridWithNode, double ODType,double rate)
        {
            IPoint InsertPoint = new PointClass();
            PublicUtil PU=new PublicUtil();
            List<IPoint> TurningPoints = this.GetTurningPoints(Pa, Grids, GridWithNode, ODType);

            #region 计算插入点
            if (Pa.FlowOutPath.Count > 0)
            {
                if (Pa.FlowOutPath[0].ePath.Count > 1)//判断该点不是起源点！
                {
                    #region 流入路径方向 sPoint和ePoint（流入路径的最后两个点）
                    #region 不考虑ODPoints
                    IPoint sPoint = new PointClass();
                    sPoint.X = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][0] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][2]) / 2;
                    sPoint.Y = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][1] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][3]) / 2;
                    #endregion

                    #region 考虑ODPoints
                    if (ODType == 1)
                    {
                        if (GridWithNode.Keys.Contains(Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]))
                        {
                            sPoint.X = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]].X;
                            sPoint.Y = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]].Y; ;
                        }
                    }
                    #endregion

                    #region 不考虑ODPoints
                    IPoint ePoint = new PointClass();
                    ePoint.X = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][0] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][2]) / 2;
                    ePoint.Y = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][1] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][3]) / 2;
                    #endregion

                    #region 考虑ODPoints
                    if (ODType == 1)
                    {
                        if (GridWithNode.Keys.Contains(Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]))
                        {
                            ePoint.X = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]].X;
                            ePoint.Y = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]].Y;
                        }
                    }
                    #endregion
                    #endregion

                    #region 获取无偏移的控制点
                    double AheadDis = PU.GetDis(sPoint, ePoint);
                    double CurDis = PU.GetDis(TurningPoints[0], TurningPoints[1]);
                    double ConDis = CurDis * rate;
                    double DisRate = -(ConDis + AheadDis) / ConDis;
                    InsertPoint = PU.GetExtendPoint(sPoint, ePoint, DisRate);
                    #endregion
                }
            }
            #endregion

            return InsertPoint;
        }

         /// <summary>
        /// 获得当前路径需嵌入的控制点（嵌入方式1-自由边控制点嵌入） 起始点进行偏移后的控制点
        /// </summary>
        /// <param name="Pa"></param>当前路径
        /// <param name="Grids"></param>Grid
        /// <param name="GridWithNode"></param>GridwithNodes
        /// <param name="ODType"></param>是否考虑ODPoints
        /// <param name="rate"></param> 控制点移动的距离
        /// GeoPrj=1表示需要做坐标变换；=0表示不需要考虑投影和坐标变换等（该参数不可靠）
        /// <returns></returns>
        public List<IPoint> GetShiftInsertControlPoint(Path Pa, Dictionary<Tuple<int, int>, List<double>> Grids, Dictionary<Tuple<int, int>, IPoint> GridWithNode, double ODType,double rate,double MaxWidth, double MinWidth, double MaxVolume, double MinVolume,int Type,int GeoPrj)
        {
            PublicUtil PU=new PublicUtil();
            FlowMapUtil FMU = new FlowMapUtil();
            List<IPoint> TurningPoints = this.GetTurningPoints(Pa, Grids, GridWithNode, ODType);
            List<IPoint> ShiftControlPoints = new List<IPoint>();

            #region 计算插入点
            if (Pa.FlowOutPath.Count > 0)
            {
                if (Pa.FlowOutPath[0].ePath.Count > 1)//判断该点不是起源点！
                {
                    #region 流入路径方向 sPoint和ePoint（流入路径的最后两个点）
                    #region 不考虑ODPoints
                    IPoint sPoint = new PointClass();
                    sPoint.X = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][0] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][2]) / 2;
                    sPoint.Y = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][1] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][3]) / 2;
                    #endregion

                    #region 考虑ODPoints
                    if (ODType == 1)
                    {
                        if (GridWithNode.Keys.Contains(Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]))
                        {
                            sPoint.X = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]].X;
                            sPoint.Y = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]].Y; 
                        }
                    }
                    #endregion

                    #region 不考虑ODPoints
                    IPoint ePoint = new PointClass();
                    ePoint.X = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][0] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][2]) / 2;
                    ePoint.Y = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][1] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][3]) / 2;
                    #endregion

                    #region 考虑ODPoints
                    if (ODType == 1)
                    {
                        if (GridWithNode.Keys.Contains(Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]))
                        {
                            ePoint.X = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]].X;
                            ePoint.Y = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]].Y;
                        }
                    }
                    #endregion
                    #endregion

                    #region 获得偏移后的起点
                    int ShiftOri = 2;//2不偏移；1向左偏移；3向右偏移(在此)
                    double ShiftDis = FMU.FlowPathShiftDis(Pa, Grids, out ShiftOri, MaxWidth, MinWidth, MaxVolume, MinVolume, Type, GeoPrj);
                    Pa.shift = ShiftOri;
                    PU.GetShiftPoint(sPoint,ePoint, TurningPoints[0], ShiftDis, ShiftOri);//第一个点的偏移（偏移直接反馈至ControlPoints[0]）
                    ShiftControlPoints.Add(TurningPoints[0]);
                    #endregion

                    #region 获取无偏移的控制点
                    double AheadDis = PU.GetDis(sPoint, ePoint);
                    double CurDis = PU.GetDis(TurningPoints[0], TurningPoints[1]);
                    double ConDis = CurDis * rate;
                    double DisRate = -(ConDis + AheadDis) / ConDis;
                    IPoint InsertPoint = PU.GetExtendPoint(sPoint, ePoint, DisRate);
                    PU.GetShiftPoint(sPoint, ePoint, InsertPoint, ShiftDis, ShiftOri);//对插入的点进行偏移
                    ShiftControlPoints.Add(InsertPoint);
                    #endregion
                }
            }
            #endregion

            return ShiftControlPoints;
        }

        /// <summary>
        /// 获得当前路径需嵌入的控制点（嵌入方式1-自由边控制点嵌入）【未考虑比例尺和投影变换】
        /// </summary>
        /// <param name="TurningPoints">原有控制点</param>
        /// <param name="sPoint">流入路径的倒数第二个点</param>
        /// <param name="ePoint">流入路径的倒数第一个点</param>
        /// <param name="ShiftDis">控制点生成的偏移距离</param>
        /// Rate 贝塞尔曲线偏移的比例
        /// <returns></returns>
        public List<IPoint> GetShiftInsertControlPoint(List<IPoint> TurningPoints, IPoint sPoint, IPoint ePoint, double ShiftDis,double rate)
        {
            List<IPoint> ShiftControlPoints = new List<IPoint>();
            PublicUtil PU = new PublicUtil();

            #region 偏移的起点
            PU.GetShiftPoint(sPoint, ePoint, TurningPoints[0], ShiftDis);//第一个点的偏移（偏移直接反馈至ControlPoints[0]）
            ShiftControlPoints.Add(TurningPoints[0]);
            #endregion

            #region 获取无偏移的控制点
            double AheadDis = PU.GetDis(sPoint, ePoint);
            double CurDis = PU.GetDis(TurningPoints[0], TurningPoints[1]);
            double ConDis = CurDis * rate;
            double DisRate = -(ConDis + AheadDis) / ConDis;
            IPoint InsertPoint = PU.GetExtendPoint(sPoint, ePoint, DisRate);
            PU.GetShiftPoint(sPoint, ePoint, InsertPoint, ShiftDis);//对插入的点进行偏移
            ShiftControlPoints.Add(InsertPoint);
            #endregion

            return ShiftControlPoints;
        }

        /// <summary>
        /// 获得当前路径需嵌入的控制点(嵌入方式2-非自由边保证平滑控制点嵌入)
        /// </summary>
        /// <param name="Pa"></param>当前路径
        /// <param name="Grids"></param>Grid
        /// <param name="GridWithNode"></param>GridwithNodes
        /// <param name="ODType"></param>是否考虑ODPoints
        /// <param name="rate"></param> 控制点移动的距离
        /// <returns></returns>
        public List<IPoint> GetInsertControlPoint_2(Path Pa, Dictionary<Tuple<int, int>, List<double>> Grids, Dictionary<Tuple<int, int>, IPoint> GridWithNode, double ODType, double ShiftDis)
        {
            List<IPoint> InsertPoints = new List<IPoint>();
            PublicUtil PU = new PublicUtil();
            List<IPoint> TurningPoints = this.GetTurningPoints(Pa, Grids, GridWithNode, ODType);

            #region 计算插入点
            if (Pa.FlowOutPath.Count > 0)
            {
                if (Pa.FlowOutPath[0].ePath.Count > 1)//判断该点不是起源点！
                {
                    #region 获取第一个控制点
                    #region 流入路径方向 sPoint和ePoint（流入路径的最后两个点）
                    #region 不考虑ODPoints
                    IPoint sPoint = new PointClass();
                    sPoint.X = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][0] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][2]) / 2;
                    sPoint.Y = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][1] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][3]) / 2;
                    #endregion

                    #region 考虑ODPoints
                    if (ODType == 1)
                    {
                        if (GridWithNode.Keys.Contains(Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]))
                        {
                            sPoint.X = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]].X;
                            sPoint.Y = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]].Y; ;
                        }
                    }
                    #endregion

                    #region 不考虑ODPoints
                    IPoint ePoint = new PointClass();
                    ePoint.X = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][0] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][2]) / 2;
                    ePoint.Y = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][1] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][3]) / 2;
                    #endregion

                    #region 考虑ODPoints
                    if (ODType == 1)
                    {
                        if (GridWithNode.Keys.Contains(Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]))
                        {
                            ePoint.X = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]].X;
                            ePoint.Y = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]].Y;
                        }
                    }
                    #endregion
                    #endregion

                    #region 获取无偏移的第一个控制点
                    double AheadDis = PU.GetDis(sPoint, ePoint);
                    double DisRate = -(ShiftDis + AheadDis) / ShiftDis;
                    IPoint InsertPoint_1 = PU.GetExtendPoint(sPoint, ePoint, DisRate);
                    #endregion
                    #endregion

                    #region 获取第二个控制点
                    #region 获取流入路径的第二个点
                    #region 不考虑ODPoints
                    IPoint kPoint = new PointClass();
                    kPoint.X = (Grids[Pa.ePath[1]][0] + Grids[Pa.ePath[1]][2]) / 2;
                    kPoint.Y = (Grids[Pa.ePath[1]][1] + Grids[Pa.ePath[1]][3]) / 2;
                    #endregion

                    #region 考虑ODPoints
                    if (ODType == 1)
                    {
                        if (GridWithNode.Keys.Contains(Pa.ePath[1]))
                        {
                            kPoint.X = GridWithNode[Pa.ePath[1]].X;
                            kPoint.Y = GridWithNode[Pa.ePath[1]].Y; ;
                        }
                    }
                    #endregion
                    #endregion

                    #region 获取无偏移的第二个控制点
                    double AheadDis_2 = PU.GetDis(ePoint,kPoint);
                    double DisRate_2 = ShiftDis / (AheadDis_2 - ShiftDis);
                    IPoint InsertPoint_2 = PU.GetExtendPoint(ePoint, kPoint, DisRate_2);
                    #endregion
                    #endregion

                    InsertPoints.Add(InsertPoint_1); InsertPoints.Add(InsertPoint_2);
                }
            }
            #endregion

            return InsertPoints;
        }

        /// <summary>
        /// 获得当前路径需嵌入的控制点(嵌入方式2-非自由边保证平滑控制点嵌入)起始点进行偏移后的控制点
        /// </summary>
        /// <param name="Pa"></param>当前路径
        /// <param name="Grids"></param>Grid
        /// <param name="GridWithNode"></param>GridwithNodes
        /// <param name="ODType"></param>是否考虑ODPoints
        /// <param name="rate"></param> 控制点移动的距离
        /// GeoPrj=1表示需要做坐标变换；=0表示不需要考虑投影和坐标变换等（该参数不可靠）
        /// <returns></returns>
        public List<IPoint> GetShiftInsertControlPoint_2(Path Pa, Dictionary<Tuple<int, int>, List<double>> Grids, Dictionary<Tuple<int, int>, IPoint> GridWithNode, double ODType, double ShiftDis, double MaxWidth, double MinWidth, double MaxVolume, double MinVolume, int Type,int GeoPrj)
        {
            PublicUtil PU = new PublicUtil();
            FlowMapUtil FMU = new FlowMapUtil();
            List<IPoint> TurningPoints = this.GetTurningPoints(Pa, Grids, GridWithNode, ODType);
            List<IPoint> ShiftControlPoints = new List<IPoint>();

            #region 计算插入点
            if (Pa.FlowOutPath.Count > 0)
            {
                if (Pa.FlowOutPath[0].ePath.Count > 1)//判断该点不是起源点！
                {
                    #region 流入路径方向 sPoint和ePoint（流入路径的最后两个点）
                    #region 不考虑ODPoints
                    IPoint sPoint = new PointClass();
                    sPoint.X = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][0] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][2]) / 2;
                    sPoint.Y = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][1] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]][3]) / 2;
                    #endregion

                    #region 考虑ODPoints
                    if (ODType == 1)
                    {
                        if (GridWithNode.Keys.Contains(Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]))
                        {
                            sPoint.X = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]].X;
                            sPoint.Y = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 2]].Y; ;
                        }
                    }
                    #endregion

                    #region 不考虑ODPoints
                    IPoint ePoint = new PointClass();
                    ePoint.X = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][0] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][2]) / 2;
                    ePoint.Y = (Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][1] + Grids[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]][3]) / 2;
                    #endregion

                    #region 考虑ODPoints
                    if (ODType == 1)
                    {
                        if (GridWithNode.Keys.Contains(Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]))
                        {
                            ePoint.X = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]].X;
                            ePoint.Y = GridWithNode[Pa.FlowOutPath[0].ePath[Pa.FlowOutPath[0].ePath.Count - 1]].Y;
                        }
                    }
                    #endregion
                    #endregion

                    #region 获取偏移后的起点
                    int ShiftOri = 2;//2不偏移；1向左偏移；3向右偏移(在此)
                    double NodeShiftDis = FMU.FlowPathShiftDis(Pa, Grids, out ShiftOri, MaxWidth, MinWidth, MaxVolume, MinVolume, Type, GeoPrj);
                    Pa.shift = ShiftOri;
                    PU.GetShiftPoint(sPoint, ePoint, TurningPoints[0], NodeShiftDis, ShiftOri);//第一个点的偏移（偏移直接反馈至ControlPoints[0]）
                    ShiftControlPoints.Add(TurningPoints[0]);
                    #endregion

                    #region 获取第一个控制点
                    double AheadDis = PU.GetDis(sPoint, ePoint);
                    double DisRate = -(ShiftDis + AheadDis) / ShiftDis;
                    IPoint InsertPoint_1 = PU.GetExtendPoint(sPoint, ePoint, DisRate);
                    PU.GetShiftPoint(sPoint, ePoint, InsertPoint_1, NodeShiftDis, ShiftOri);
                    ShiftControlPoints.Add(InsertPoint_1);
                    #endregion

                    #region 获取第二个控制点
                    double AheadDis_2 = PU.GetDis(TurningPoints[0], TurningPoints[1]);//TurningPoints[0]已偏移
                    double DisRate_2 = ShiftDis / (AheadDis_2 - ShiftDis);
                    IPoint InsertPoint_2 = PU.GetExtendPoint(TurningPoints[0], TurningPoints[1], DisRate_2);
                    ShiftControlPoints.Add(InsertPoint_2);
                    #endregion
                }
            }
            #endregion

            return ShiftControlPoints;
        }

        /// <summary>
        /// 得当前路径需嵌入的控制点(嵌入方式2-非自由边保证平滑控制点嵌入)起始点进行偏移后的控制点
        /// </summary>
        /// <param name="TurningPoints">原有控制点</param>
        /// <param name="sPoint">流入路径的倒数第二个点</param>
        /// <param name="ePoint">流入路径的倒数第一个点</param>
        /// <param name="NodeShiftDis"></param>起点需偏移的距离
        /// <param name="shiftDis"></param> 控制点生成的控制点偏移距离比例
        /// <returns></returns>
        public List<IPoint> GetShiftInsertControlPoint_2(List<IPoint> TurningPoints, IPoint sPoint, IPoint ePoint, double NodeShiftDis, double ShiftDisRate)
        {
            List<IPoint> ShiftControlPoints = new List<IPoint>();
            PublicUtil PU = new PublicUtil();

            #region 偏移的起点
            PU.GetShiftPoint(sPoint, ePoint, TurningPoints[0], NodeShiftDis);//第一个点的偏移（偏移直接反馈至ControlPoints[0]）
            ShiftControlPoints.Add(TurningPoints[0]);
            #endregion

            double AheadDis_2 = PU.GetDis(TurningPoints[0], TurningPoints[1]);
            double ShiftDis = AheadDis_2 * ShiftDisRate;

            #region 获取无偏移的第一个控制点
            double AheadDis = PU.GetDis(sPoint, ePoint);
            double DisRate = -(ShiftDis + AheadDis) / ShiftDis;
            IPoint InsertPoint_1 = PU.GetExtendPoint(sPoint, ePoint, DisRate);
            PU.GetShiftPoint(sPoint, ePoint, InsertPoint_1, NodeShiftDis);
            ShiftControlPoints.Add(InsertPoint_1);
            #endregion

            #region 获取无偏移的第二个控制点
            //double AheadDis_2 = PU.GetDis(TurningPoints[0], TurningPoints[1]);
            double DisRate_2 = ShiftDis / (AheadDis_2 - ShiftDis);
            IPoint InsertPoint_2 = PU.GetExtendPoint(TurningPoints[0], TurningPoints[1], DisRate_2);
            ShiftControlPoints.Add(InsertPoint_2);
            #endregion

            return ShiftControlPoints;
        }

        /// <summary>
        /// 获得给定Pa的宽度 路径是Path
        /// Debiasi(2014)线性
        /// Sun(2016) 对数；Sun(2018)未知
        /// Buchin et al. (2011);线性
        /// Nocaj, A., & Brandes, U. (2013) 线性
        /// </summary>
        /// <param name="Pa">当前路径</param>
        /// MaxWidth最大宽度
        /// MinWidth最小宽度
        /// MaxVolume最大流量
        /// MinVolume最小流量
        /// Type=0宽度三角函数变化；Type=1宽度近似线性变化；Type=2宽度线性变化；
        /// <returns></returns>
        public double GetWidth(Path Pa,double MaxWidth,double MinWidth,double MaxVolume,double MinVolume,int Type)
        {
            double Width = 0;

            //三角函数变化
            if (Type == 0)
            {
                Width = Math.Sin((Pa.Volume - MinVolume) / (MaxVolume - MinVolume) * Math.PI / 2) * (MaxWidth - MinWidth) + MinWidth;
            }

            //近似线性变化
            if (Type == 1)
            {
                Width = (Pa.Volume - MinVolume) / (MaxVolume - MinVolume) * (MaxWidth - MinWidth) + MinWidth;
            }

            //线性变化
            if (Type == 2)
            {
                Width = Pa.Volume / MinVolume * MinWidth;
            }

            return Width;
        }

        /// <summary>
        /// 获得给定Pa的宽度
        /// Debiasi(2014)线性
        /// Sun(2016) 对数；Sun(2018)未知
        /// Buchin et al. (2011);线性
        /// Nocaj, A., & Brandes, U. (2013) 线性
        /// </summary>
        /// <param name="Pa">当前路径</param> 路径是PolylineObject
        /// MaxWidth最大宽度
        /// MinWidth最小宽度
        /// MaxVolume最大流量
        /// MinVolume最小流量
        /// Type=0宽度三角函数变化；Type=1宽度近似线性变化；Type=2宽度线性变化；
        /// <returns></returns>
        public double GetWidth(PolylineObject Pa, double MaxWidth, double MinWidth, double MaxVolume, double MinVolume, int Type)
        {
            double Width = 0;

            //三角函数变化
            if (Type == 0)
            {
                Width = Math.Sin((Pa.Volume - MinVolume) / (MaxVolume - MinVolume) * Math.PI / 2) * (MaxWidth - MinWidth) + MinWidth;
            }

            //近似线性变化
            if (Type == 1)
            {
                Width = (Pa.Volume - MinVolume) / (MaxVolume - MinVolume) * (MaxWidth - MinWidth) + MinWidth;
            }

            //线性变化
            if (Type == 2)
            {
                Width = Pa.Volume / MinVolume * MinWidth;
            }

            return Width;
        }
    }
}
