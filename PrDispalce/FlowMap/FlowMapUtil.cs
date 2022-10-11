using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

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
    /// UtilForFlowMap
    /// </summary>
    class FlowMapUtil
    {
        PrDispalce.FlowMap.PublicUtil Pu = new PublicUtil();
        FlowSup Fs = new FlowSup();
        public AxMapControl pMapControl;

        /// <summary>
        /// 获得图层的范围
        /// </summary>
        /// <param name="pFeatureLayer"></param>
        /// <returns></returns>
        public double[] GetExtend(IFeatureLayer pFeatureLayer)
        {
            double[] ExtendValue = new double[4];
            IGeoDataset pGeoDataset = pFeatureLayer as IGeoDataset;
            IEnvelope Extend = pGeoDataset.Extent;
            ExtendValue[0] = Extend.XMin; ExtendValue[1] = Extend.YMin; ExtendValue[2] = Extend.XMax; ExtendValue[3] = Extend.YMax;

            return ExtendValue;
        }

        /// <summary>
        /// 获取给定的ODPoints
        /// </summary>
        /// <param name="pFeatureClass"></param>
        /// <param name="OriginPoint"></param>起点
        /// <param name="DesPoints"></param>终点
        /// <param name="AllPoints"></param>所有点
        /// <param name="PointFlow"></param>各点（des）流量统计
        public void GetOD(IFeatureClass pFeatureClass, IPoint OriginPoint, List<IPoint> DesPoints, List<IPoint> AllPoints, Dictionary<IPoint, double> PointFlow, ISpatialReference ISR)
        {
            IFeatureCursor pFeatureCursor = pFeatureClass.Update(null, true);
            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                double Loc = Pu.GetValue(pFeature, "ID");
                IPoint nPoint = pFeature.Shape as IPoint;
                //nPoint.SpatialReference = ISR;//给点设置投影坐标系，与Map保持一致
                nPoint.Project(ISR);

                if (Pu.GetValue(pFeature, "ID") == 0)
                {
                    double Flow = Pu.GetValue(pFeature, "FlowOut");
                    //OriginPoint.SpatialReference = ISR;//给点设置投影坐标系，与Map保持一致
                    OriginPoint.X = nPoint.X;///pfeatureCursor本身可能存在问题
                    OriginPoint.Y = nPoint.Y;
                    AllPoints.Add(OriginPoint);
                    PointFlow.Add(OriginPoint, Flow);
                }

                else
                {
                    double Flow = Pu.GetValue(pFeature, "FlowOut");
                    IPoint rPoint = new PointClass();///pfeatureCursor本身可能存在问题
                    //rPoint.SpatialReference = ISR;    //给点设置投影坐标系，与Map保持一致                            
                    rPoint.X = nPoint.X;
                    rPoint.Y = nPoint.Y;
                    DesPoints.Add(rPoint);
                    AllPoints.Add(rPoint);
                    PointFlow.Add(rPoint, Flow);
                }

                pFeature = pFeatureCursor.NextFeature();
            }
        }

        /// <summary>
        /// 获取Path polylines
        /// </summary>
        /// <param name="pFeatureClass"></param>
        /// <returns></returns>
        public List<PolylineObject> GetPathPolyline(IFeatureClass pFeatureClass)
        {
            List<PolylineObject> PathPolylines = new List<PolylineObject>();
            FlowDraw FD = new FlowDraw();

            IFeatureCursor pFeatureCursor = pFeatureClass.Update(null, true);
            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                #region 获取空间属性（只将转折点表达为线节点）
                IPolyline pPolyline = pFeature.Shape as IPolyline;
                List<TriNode> NodeList = new List<TriNode>();
                IPointCollection pPointColl = pPolyline as IPointCollection;
                for (int i = 0; i < pPointColl.PointCount; i++)
                {
                    TriNode CacheNode = new TriNode();
                    CacheNode.X = pPointColl.get_Point(i).X;
                    CacheNode.Y = pPointColl.get_Point(i).Y;
                    NodeList.Add(CacheNode);
                }

                List<TriNode> TurningTriNode = FD.GetTurningPoints(NodeList);//获取转折点               
                #endregion

                #region 获取其它属性
                double ShiftDis = Pu.GetValue(pFeature, "SDis");//偏移距离
                double Width = Pu.GetValue(pFeature, "Width");//宽度
                double Volume = Pu.GetValue(pFeature, "Volume");//流量
                int FlowOutId = Pu.GetintValue(pFeature, "FOID");//流出的路径编号
                int FlowOutCount = Pu.GetintValue(pFeature, "FlowOut");
                int FlowInCount = Pu.GetintValue(pFeature, "FlowIn");
                #endregion

                PolylineObject CachePoLine = new PolylineObject(NodeList);
                CachePoLine.ShiftDis = ShiftDis;
                CachePoLine.Volume = Volume;
                CachePoLine.FlowOutId = FlowOutId;
                CachePoLine.SylWidth = Width;
                CachePoLine.FlowOut = FlowOutCount;
                CachePoLine.FlowIn = FlowInCount;
                PathPolylines.Add(CachePoLine);
                
                pFeature = pFeatureCursor.NextFeature();
            }

            this.PathOrganized(PathPolylines);//对河流的系统进行组织！！！
            return PathPolylines;
        }

        /// <summary>
        /// 获得路径的最大流量
        /// </summary>
        /// <param name="PLList"></param>
        /// <returns></returns>
        public double GetMaxVolume(List<PolylineObject> PLList)
        {
            List<double> VolumeValues = this.GetAllVolume(PLList);
            return VolumeValues.Max();
        }

        /// <summary>
        /// 获得路径的最小流量
        /// </summary>
        /// <param name="PLList"></param>
        /// <returns></returns>
        public double GetMinVolume(List<PolylineObject> PLList)
        {
            List<double> VolumeValues = this.GetAllVolume(PLList);
            double CacheMin= VolumeValues.Min();
            if (CacheMin == 0)//起点也可能包含在内
            {
                VolumeValues.Remove(CacheMin);
                return VolumeValues.Min();
            }

            return CacheMin;
        }

        /// <summary>
        /// 获得每条路径流量的列表
        /// </summary>
        /// <param name="PLList"></param>
        /// <returns></returns>
        public List<double> GetAllVolume(List<PolylineObject> PLList)
        {
            List<double> VolumeValues = new List<double>();
            for (int i = 0; i < PLList.Count; i++)
            {
                VolumeValues.Add(PLList[i].Volume);
            }

            return VolumeValues;
        }

        /// <summary>
        /// 依据线状要素的流入路径对Flow网状结构进行组织
        /// </summary>
        /// <param name="PLList"></param>
        public void PathOrganized(List<PolylineObject> PLList)
        {
            for (int i = 0; i < PLList.Count; i++)
            {
                for (int j = 0; j < PLList.Count; j++)
                {
                    if (j != i)
                    {
                        if (PLList[j].FlowOutId == i)
                        {
                            PLList[i].FlowInIDList.Add(j);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取给定图层的Features
        /// </summary>
        /// <param name="pFeatureLayers"></param>
        /// <returns></returns>
        public List<Tuple<IGeometry, esriGeometryType>> GetFeatures(List<IFeatureLayer> pFeatureLayers)
        {
            List<Tuple<IGeometry, esriGeometryType>> Features = new List<Tuple<IGeometry, esriGeometryType>>();

            #region 获取Featuers
            for (int i = 0; i < pFeatureLayers.Count; i++)
            {
                IFeatureClass pFeatureClass = pFeatureLayers[i].FeatureClass;
                for (int j = 0; j < pFeatureClass.FeatureCount(null); j++)
                {
                    IFeature pFeature = pFeatureClass.GetFeature(j);

                    IGeometry pGeometry = pFeature.Shape;
                    Tuple<IGeometry, esriGeometryType> CacheTuple = new Tuple<IGeometry, esriGeometryType>(pGeometry, pFeatureLayers[i].FeatureClass.ShapeType);
                    Features.Add(CacheTuple);
                }
            }
            #endregion

            return Features;
        }

        /// <summary>
        /// 获取总流量各点（des）流量统计
        /// </summary>
        /// <param name="PointFlow"></param>
        /// <returns></returns>
        public double GetAllVolume(Dictionary<IPoint, double> PointFlow)
        {
            return PointFlow.Values.Sum();
        }

        /// <summary>
        /// 获取最大流量各点（des）流量统计
        /// </summary>
        /// <param name="PointFlow"></param>
        /// <returns></returns>
        public double GetMaxVolume(Dictionary<IPoint, double> PointFlow)
        {
            return PointFlow.Values.Max();
        }

        /// <summary>
        /// 获取最小流量
        /// </summary>
        /// <param name="PointFlow"></param>各点（des）流量统计
        /// <returns></returns>
        public double GetMinVolume(Dictionary<IPoint, double> PointFlow)
        {
            return PointFlow.Values.Min();
        }

        /// <summary>
        /// 判断是否满足角度约束条件
        /// </summary>
        /// <param name="CacheShortPath"></param>待判断路径
        /// <param name="Path"></param>路径连接的主流路径
        /// <param name="Grids"></param>网格编码
        /// <returns></returns>true表示不满足角度约束限制
        /// false表示满足角度约束显示
        public bool AngleContraint(List<Tuple<int, int>> CacheShortPath, Path Path, Dictionary<Tuple<int, int>, List<double>> Grids)
        {
            Tuple<int, int> FromGrid = null;
            Tuple<int, int> MidGrid = null;
            Tuple<int, int> ToGrid = null;
            double CacheAngle = 0;
            if (CacheShortPath != null && CacheShortPath.Count >= 2 && Path.ePath.Count >= 2)
            {
                FromGrid = CacheShortPath[1];
                MidGrid = CacheShortPath[0];
                ToGrid = Path.ePath[Path.ePath.Count - 2];

                TriNode FromPoint = new TriNode();
                TriNode MidPoint = new TriNode();
                TriNode ToPoint = new TriNode();

                FromPoint.X = (Grids[FromGrid][0] + Grids[FromGrid][2]) / 2;//流入路径的第二个点
                FromPoint.Y = (Grids[FromGrid][1] + Grids[FromGrid][3]) / 2;

                MidPoint.X = (Grids[MidGrid][0] + Grids[MidGrid][2]) / 2;//流入路径的第一个点
                MidPoint.Y = (Grids[MidGrid][1] + Grids[MidGrid][3]) / 2;

                ToPoint.X = (Grids[ToGrid][0] + Grids[ToGrid][2]) / 2;//主路径的倒数第二个点
                ToPoint.Y = (Grids[ToGrid][1] + Grids[ToGrid][3]) / 2;

                CacheAngle = Pu.GetAngle(MidPoint, ToPoint, FromPoint);

                //if (CacheAngle > 3.1415927 / 3 && CacheAngle < 2 * 3.1415926 / 3)
                if (CacheAngle < 2 * 3.1415926 / 3)
                {
                    return true;
                }
            }

            else
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// 判断是否满足角度约束条件（计算流入的角度阈值）
        /// </summary>
        /// <param name="CacheShortPath"></param>待判断路径
        /// <param name="Path"></param>路径连接的主流路径
        /// <param name="Grids"></param>网格编码
        /// <returns></returns>true表示不满足角度约束限制
        /// false表示满足角度约束显示
        public double AnglePath(List<Tuple<int, int>> CacheShortPath, Path Path, Dictionary<Tuple<int, int>, List<double>> Grids)
        {
            Tuple<int, int> FromGrid = null;
            Tuple<int, int> MidGrid = null;
            Tuple<int, int> ToGrid = null;
            double CacheAngle = 0;
            if (CacheShortPath != null && CacheShortPath.Count >= 2 && Path.ePath.Count >= 2)
            {
                FromGrid = CacheShortPath[1];
                MidGrid = CacheShortPath[0];
                ToGrid = Path.ePath[Path.ePath.Count - 2];

                TriNode FromPoint = new TriNode();
                TriNode MidPoint = new TriNode();
                TriNode ToPoint = new TriNode();

                FromPoint.X = (Grids[FromGrid][0] + Grids[FromGrid][2]) / 2;
                FromPoint.Y = (Grids[FromGrid][1] + Grids[FromGrid][3]) / 2;

                MidPoint.X = (Grids[MidGrid][0] + Grids[MidGrid][2]) / 2;
                MidPoint.Y = (Grids[MidGrid][1] + Grids[MidGrid][3]) / 2;

                ToPoint.X = (Grids[ToGrid][0] + Grids[ToGrid][2]) / 2;
                ToPoint.Y = (Grids[ToGrid][1] + Grids[ToGrid][3]) / 2;

                CacheAngle = Pu.GetAngle(MidPoint, ToPoint, FromPoint);          
            }

            return CacheAngle;
        }

        /// <summary>
        /// 判断一条给定的路径是否直线汇入主流（通过点来判断是否线性汇入）
        /// </summary>
        /// <param name="Path">给定路径-路径是按格网来编码的</param>
        /// AngleThr 角度阈值条件
        /// <returns>False表示非直线汇入；True表示直线汇入</returns>
        public bool FlowPathOnLine(Path Path, Dictionary<Tuple<int, int>, List<double>> Grids,double AngleThr)
        {
            bool OnLineLable = false;
            PrDispalce.FlowMap.PublicUtil PU = new PublicUtil();
            for (int i = 0; i < Path.FlowOutPath.Count; i++)
            {
                if (Path.FlowOutPath[i].ePath.Count > 1) //判断该点不是起源点！
                {
                    IPoint CachePoint = new PointClass();
                    CachePoint.X = (Grids[Path.FlowOutPath[i].ePath[Path.FlowOutPath[i].ePath.Count - 2]][0] + Grids[Path.FlowOutPath[i].ePath[Path.FlowOutPath[i].ePath.Count - 2]][2]) / 2;
                    CachePoint.Y = (Grids[Path.FlowOutPath[i].ePath[Path.FlowOutPath[i].ePath.Count - 2]][1] + Grids[Path.FlowOutPath[i].ePath[Path.FlowOutPath[i].ePath.Count - 2]][3]) / 2;

                    IPoint sPoint = new PointClass();
                    sPoint.X = (Grids[Path.ePath[0]][0] + Grids[Path.ePath[0]][2]) / 2;
                    sPoint.Y = (Grids[Path.ePath[0]][1] + Grids[Path.ePath[0]][3]) / 2;

                    IPoint ePoint = new PointClass();
                    ePoint.X = (Grids[Path.ePath[1]][0] + Grids[Path.ePath[1]][2]) / 2;
                    ePoint.Y = (Grids[Path.ePath[1]][1] + Grids[Path.ePath[1]][3]) / 2;

                    double Angle = PU.GetAngle(sPoint, CachePoint, ePoint);

                    if (Math.Abs(Angle - Math.PI) < AngleThr)
                    {
                        OnLineLable = true;//共线
                    }
                }
            }

            return OnLineLable;
        }


        /// <summary>
        /// 判断一条给定的路径是否直线汇入主流（通过点来判断是否线性汇入）
        /// </summary>
        /// <param name="Path">给定路径-路径是按格网来编码的</param>
        /// AngleThr 角度阈值条件
        /// <returns>False表示非直线汇入；True表示直线汇入</returns>
        public bool FlowPathOnLine_2(PolylineObject TarPL, List<PolylineObject> PLList, double AngleThr)
        {
            bool OnLineLable = false;
            PublicUtil PU = new PublicUtil();
            TriNode sPoint = TarPL.PointList[0];
            TriNode ePoint = TarPL.PointList[1];
            if (TarPL.FlowOut > 0)//不是起点
            {
                if (PLList[TarPL.FlowOutId].FlowOut > 0)//汇入的不是起点
                {
                    TriNode CachePoint = PLList[TarPL.FlowOutId].PointList[PLList[TarPL.FlowOutId].PointList.Count - 2];

                    double Angle = PU.GetAngle(sPoint, CachePoint, ePoint);

                    if (Math.Abs(Angle - Math.PI) < AngleThr)
                    {
                        OnLineLable = true;//共线
                    }
                }
            }

            return OnLineLable;
        }
        /// <summary>
        /// 判断一条给定的路径是否直线汇入主流(通过矩阵格网来判断是否线性汇入)
        /// </summary>
        /// <param name="Path">给定路径-路径是按格网来编码的</param>
        /// AngleThr 角度阈值条件
        /// <returns>False表示非直线汇入；True表示直线汇入</returns>
        public bool FlowPathOnLine_2(Path Path, Dictionary<Tuple<int, int>, List<double>> Grids, double AngleThr)
        {
            bool OnLineLable = true;
            PrDispalce.FlowMap.PublicUtil PU = new PublicUtil();
            for (int i = 0; i < Path.FlowOutPath.Count; i++)
            {
                if (Path.FlowOutPath[i].ePath.Count > 1) //判断该点不是起源点！
                {
                    int add11 = Path.ePath[1].Item1 - Path.ePath[0].Item1;
                    int add12 = Path.ePath[1].Item2 - Path.ePath[0].Item2;

                    int add21 = Path.FlowOutPath[0].ePath[Path.FlowOutPath[0].ePath.Count - 1].Item1 - Path.FlowOutPath[0].ePath[Path.FlowOutPath[0].ePath.Count - 2].Item1;
                    int add22 = Path.FlowOutPath[0].ePath[Path.FlowOutPath[0].ePath.Count - 1].Item2 - Path.FlowOutPath[0].ePath[Path.FlowOutPath[0].ePath.Count - 2].Item2;

                    if (add11 != add21 || add12 != add22)
                    {
                        return false;
                    }
                }
            }

            return OnLineLable;
        }

        /// <summary>
        /// 判断Point3是在Point1Point2的左侧还是右侧（方向是Point1至Point2）
        /// </summary>
        /// <param name="Point1"></param>
        /// <param name="Point2"></param>
        /// <param name="Point3"></param>
        /// <returns></returns>=1左侧；=2线上；=3右侧
        public int LeftOrRight(IPoint Point1, IPoint Point2, IPoint Point3)
        {
            double S = (Point1.X - Point3.X) * (Point2.Y - Point3.Y) - (Point1.Y - Point3.Y) * (Point2.X - Point3.X);

            if (S > 0.01)//大于0左侧
            {
                return 1;
            }
            else if (S < -0.01)
            {
                return 3;
            }
            else//小于0右侧
            {
                return 2;
            }
        }

        /// <summary>
        /// 判断Point3是在Point1Point2的左侧还是右侧（方向是Point1至Point2）
        /// </summary>
        /// <param name="Point1"></param>
        /// <param name="Point2"></param>
        /// <param name="Point3"></param>
        /// <returns></returns>=1左侧；=2线上；=3右侧
        public int LeftOrRight(TriNode Point1, TriNode Point2, TriNode Point3)
        {
            double S = (Point1.X - Point3.X) * (Point2.Y - Point3.Y) - (Point1.Y - Point3.Y) * (Point2.X - Point3.X);

            if (S > 0.01)//大于0左侧
            {
                return 1;
            }
            else if (S < -0.01)
            {
                return 3;
            }
            else//小于0右侧
            {
                return 2;
            }
        }

        /// <summary>
        /// 判断一条给定路径是在左侧还是右侧汇入主流（方向为Path的前至后）
        /// </summary>
        /// <param name="Path"></param>
        /// <param name="Grids"></param>
        /// <returns></returns>=1左侧；=2线上；=3右侧
        public int FlowPathLeftOrRight(Path Path, Dictionary<Tuple<int, int>, List<double>> Grids)
        {
            if (Path.FlowOutPath.Count > 0)
            {
                if (Path.FlowOutPath[0].ePath.Count > 1)//如果汇入不是起点
                {
                    IPoint sPoint = new PointClass();
                    sPoint.X = (Grids[Path.FlowOutPath[0].ePath[Path.FlowOutPath[0].ePath.Count - 2]][0] + Grids[Path.FlowOutPath[0].ePath[Path.FlowOutPath[0].ePath.Count - 2]][2]) / 2;//倒数第二个点
                    sPoint.Y = (Grids[Path.FlowOutPath[0].ePath[Path.FlowOutPath[0].ePath.Count - 2]][1] + Grids[Path.FlowOutPath[0].ePath[Path.FlowOutPath[0].ePath.Count - 2]][3]) / 2;

                    IPoint ePoint = new PointClass();
                    ePoint.X = (Grids[Path.FlowOutPath[0].ePath[Path.FlowOutPath[0].ePath.Count - 1]][0] + Grids[Path.FlowOutPath[0].ePath[Path.FlowOutPath[0].ePath.Count - 1]][2]) / 2;//倒数第一个点
                    ePoint.Y = (Grids[Path.FlowOutPath[0].ePath[Path.FlowOutPath[0].ePath.Count - 1]][1] + Grids[Path.FlowOutPath[0].ePath[Path.FlowOutPath[0].ePath.Count - 1]][3]) / 2;

                    IPoint CurPoint = new PointClass();
                    CurPoint.X = (Grids[Path.ePath[1]][0] + Grids[Path.ePath[1]][2]) / 2;
                    CurPoint.Y = (Grids[Path.ePath[1]][1] + Grids[Path.ePath[1]][3]) / 2;

                    int LeftOrRight=this.LeftOrRight(sPoint, ePoint, CurPoint);
                    Path.LOrR = LeftOrRight;
                    return LeftOrRight;
                }

                else //汇入的是起点
                {
                    return 2;
                }
            }

            else
            {
                return 2;
            }
        }

        /// <summary>
        /// 获取一条路径汇入主流的方向（以Grid计算）（以流入路径为起点，旋转至流出路径）
        /// </summary>
        /// <param name="Path"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public double GetFlowInAngle(Path Path, Dictionary<Tuple<int, int>, List<double>> Grids)
        {
            double Angle = 0;
            PrDispalce.FlowMap.PublicUtil PU = new PublicUtil();
       
            if (Path.FlowOutPath.Count > 0)//路径不是起点
            { 
                #region 如果汇入的点不是起点
                if (Path.FlowOutPath[0].ePath.Count > 1) //判断该点不是起源点！
                {
                    IPoint CachePoint = new PointClass();
                    CachePoint.X = (Grids[Path.FlowOutPath[0].ePath[Path.FlowOutPath[0].ePath.Count - 2]][0] + Grids[Path.FlowOutPath[0].ePath[Path.FlowOutPath[0].ePath.Count - 2]][2]) / 2;
                    CachePoint.Y = (Grids[Path.FlowOutPath[0].ePath[Path.FlowOutPath[0].ePath.Count - 2]][1] + Grids[Path.FlowOutPath[0].ePath[Path.FlowOutPath[0].ePath.Count - 2]][3]) / 2;

                    IPoint sPoint = new PointClass();
                    sPoint.X = (Grids[Path.ePath[0]][0] + Grids[Path.ePath[0]][2]) / 2;
                    sPoint.Y = (Grids[Path.ePath[0]][1] + Grids[Path.ePath[0]][3]) / 2;

                    IPoint ePoint = new PointClass();
                    ePoint.X = (Grids[Path.ePath[1]][0] + Grids[Path.ePath[1]][2]) / 2;
                    ePoint.Y = (Grids[Path.ePath[1]][1] + Grids[Path.ePath[1]][3]) / 2;

                    Angle = PU.GetAngle(sPoint, CachePoint, ePoint);
                }
               #endregion

                #region 如果汇入的点是起点(计算与水平线的夹角)
                else
                {
                    IPoint sPoint = new PointClass();
                    sPoint.X = (Grids[Path.ePath[0]][0] + Grids[Path.ePath[0]][2]) / 2;
                    sPoint.Y = (Grids[Path.ePath[0]][1] + Grids[Path.ePath[0]][3]) / 2;

                    IPoint CachePoint = new PointClass();
                    CachePoint.X = sPoint.X - 5;
                    CachePoint.Y = sPoint.Y;

                    IPoint ePoint = new PointClass();
                    ePoint.X = (Grids[Path.ePath[1]][0] + Grids[Path.ePath[1]][2]) / 2;
                    ePoint.Y = (Grids[Path.ePath[1]][1] + Grids[Path.ePath[1]][3]) / 2;

                    Angle = PU.GetAngle(sPoint, CachePoint, ePoint);
                }
                #endregion
            }

            return Angle;
        }

        /// <summary>
        /// 获得Path偏移的距离和方向
        /// </summary>
        /// <param name="Path"></param>
        /// <param name="Grids"></param>
        /// ShiftLabel=2不偏移；1向左偏移；3向右偏移(在此)
        /// MaxWidth最大宽度
        /// MinWidth最小宽度
        /// MaxVolume最大流量
        /// MinVolume最小流量
        /// Type=0宽度三角函数变化；Type=1宽度近似线性变化；Type=2宽度线性变化；
        /// GeoPrj=1表示存在投影变换；GeoPrj=0表示不存在投影和比例尺，无需坐标变换（该参数不可靠）
        /// <returns></returns>
        public double FlowPathShiftDis(Path Path, Dictionary<Tuple<int, int>, List<double>> Grids, out int ShifiLabel, double MaxWidth, double MinWidth, double MaxVolume, double MinVolume, int Type,int GeoPrj)
        {
            ShifiLabel = 2; double PI = 3.1415926;
            double ShiftDis=0;
            PrDispalce.FlowMap.PublicUtil PU = new PublicUtil();
            FlowDraw FD = new FlowDraw();

            #region 汇入不是起点(若汇入的是起点，则不偏移)
            double CurAngle = 0;
            if (Path.FlowOutPath.Count > 0) //不是起点作为路径
            {
                if (Path.FlowOutPath[0].ePath.Count > 1)//如果汇入不是起点
                {
                    #region 获得当前路径的位置
                    Dictionary<double, Path> AngleDic = new Dictionary<double, Path>();//能这样初始化的原因是本研究对于同一个节点不存在相同方向的Path
                    for (int i = 0; i < Path.FlowOutPath[0].FlowInPath.Count; i++)
                    {
                        double Angle = this.GetFlowInAngle(Path.FlowOutPath[0].FlowInPath[i], Grids);//角度范围[0,PI] 
                        if (this.FlowPathLeftOrRight(Path.FlowOutPath[0].FlowInPath[i], Grids) == 2 || Angle < 0.01)
                        {
                            Angle = PI;
                        }
                        else if (this.FlowPathLeftOrRight(Path.FlowOutPath[0].FlowInPath[i], Grids) == 3)//如果有是右侧，则进行换算
                        {
                            Angle = 2 * PI - Angle;
                        }

                        if (Path.FlowOutPath[0].FlowInPath[i] == Path)
                        {
                            CurAngle = Angle;                        
                        }

                        if (!AngleDic.ContainsKey(Angle))
                        {
                            AngleDic.Add(Angle, Path.FlowOutPath[0].FlowInPath[i]);
                        }
                    }

                    List<double> CacheAngleList = AngleDic.Keys.ToList(); CacheAngleList.Sort();//排序（从小到大）
                    int Index = CacheAngleList.IndexOf(CurAngle);//获得当前角度的标号
                    #endregion

                    #region 计算shiftDis
                    double SumWidth = 0;
                    for (int i = 0; i < Index; i++)
                    {
                        double CacheWidth = FD.GetWidth(AngleDic[CacheAngleList[i]], MaxWidth, MinWidth, MaxVolume, MinVolume, Type);//获得流入路径的宽度
                        SumWidth = SumWidth + CacheWidth;
                    }

                    double CurWidth = FD.GetWidth(Path, MaxWidth, MinWidth, MaxVolume, MinVolume, Type);
                    double AheadWidth = FD.GetWidth(Path.FlowOutPath[0], MaxWidth, MinWidth, MaxVolume, MinVolume, Type);//获得流入路径的宽度
                    if ((0.5 * CurWidth + SumWidth) > 0.5 * AheadWidth)
                    {
                        ShiftDis = 0.5 * CurWidth + SumWidth - 0.5 * AheadWidth;
                        ShifiLabel = 3;
                    }
                    else
                    {
                        ShiftDis = 0.5 * AheadWidth - 0.5 * CurWidth - SumWidth;
                        ShifiLabel = 1;
                    }
                    #endregion
                }
            }
            #endregion

            ///这里需要额外考虑！！！
            //if (GeoPrj == 1)//表示存在投影转换
            //{
            //    ShiftDis = ShiftDis / 11.2;
            //}

            return ShiftDis;
        }

        /// <summary>
        /// 计算偏移距离
        /// ShiftLabel=2不偏移；1向左偏移；3向右偏移(在此)
        /// 若>0，向左偏移；若<0，向右偏移
        /// </summary>
        /// <param name="TarPL">待偏移的路径</param>
        /// <param name="PLList">路径列表</param>
        /// Scale=0 不考虑投影变换；Scale>0 考虑投影变换！！
        /// 
        /// <returns></returns>
        public double FlowPathShiftDis(PolylineObject TarPL, List<PolylineObject> PLList,double Scale)
        {
            double NodeShiftDis = 0; double PI = 3.1415926;
            PrDispalce.FlowMap.PublicUtil PU = new PublicUtil();
            FlowDraw FD = new FlowDraw();

            #region 计算过程
            double CurAngle = 0;
            if (TarPL.FlowOut > 0)//1. 路径不是起点；
            {
                if (PLList[TarPL.FlowOutId].FlowOut > 0)//2.路径不是汇入起点的路径
                {
                    #region 获得当前路径的位置
                    Dictionary<double, PolylineObject> AngleDic = new Dictionary<double, PolylineObject>();//能这样初始化的原因是本研究对于同一个节点不存在相同方向的Path
                    for (int i = 0; i < PLList[TarPL.FlowOutId].FlowInIDList.Count; i++)
                    {
                        TriNode sPoint = PLList[PLList[TarPL.FlowOutId].FlowInIDList[i]].PointList[0];
                        TriNode ePoint = PLList[PLList[TarPL.FlowOutId].FlowInIDList[i]].PointList[1];
                        TriNode CachePoint = PLList[TarPL.FlowOutId].PointList[PLList[TarPL.FlowOutId].PointList.Count - 2];

                        double Angle = PU.GetAngle(sPoint, CachePoint, ePoint);//角度范围[0,PI] 
                        if (this.LeftOrRight(CachePoint, sPoint, ePoint) == 2 || Angle < 0.01)//直线汇入，角度为180
                        {
                            Angle = PI;
                        }
                        else if (this.LeftOrRight(CachePoint, sPoint, ePoint) == 3)//如果有是右侧，则进行换算
                        {
                            Angle = 2 * PI - Angle;
                        }

                        if (PLList[PLList[TarPL.FlowOutId].FlowInIDList[i]] == TarPL)
                        {
                            CurAngle = Angle;
                        }

                        if (!AngleDic.ContainsKey(Angle))
                        {
                            AngleDic.Add(Angle, PLList[PLList[TarPL.FlowOutId].FlowInIDList[i]]);
                        }
                    }

                    List<double> CacheAngleList = AngleDic.Keys.ToList(); CacheAngleList.Sort();//排序（从小到大）
                    int Index = CacheAngleList.IndexOf(CurAngle);//获得当前角度的标号
                    #endregion

                    #region 计算shiftDis
                    double SumWidth = 0;
                    for (int i = 0; i < Index; i++)
                    {
                        double CacheWidth = AngleDic[CacheAngleList[i]].SylWidth;//获得流入路径的宽度
                        SumWidth = SumWidth + CacheWidth;
                    }

                    double CurWidth = TarPL.SylWidth;
                    double AheadWidth = PLList[TarPL.FlowOutId].SylWidth;//获得流入路径的宽度
                    if ((0.5 * CurWidth + SumWidth) > 0.5 * AheadWidth)
                    {
                        NodeShiftDis = 0.5 * CurWidth + SumWidth - 0.5 * AheadWidth;
                        NodeShiftDis = NodeShiftDis * (-1);//向右偏移
                        //ShifiLabel = 3;
                    }
                    else
                    {
                        NodeShiftDis = 0.5 * AheadWidth - 0.5 * CurWidth - SumWidth;//向左偏移
                        //ShifiLabel = 1;
                    }
                    #endregion
                }
            }
            #endregion

            #region 考虑比例尺和投影时的偏移
            if (Scale > 0)
            {
                double RealWidth = Scale * NodeShiftDis / 1000000;//实地距离（单位为千米）
                string unitDescriptor;
                if(pMapControl!=null)
                {
                     NodeShiftDis=this.ConvertLengthToMapDis(RealWidth,pMapControl.MapUnits,out unitDescriptor);
                }
            }
            #endregion

            return NodeShiftDis;
        }

        /// <summary>
        /// 将给定的距离转换为地图表达的单位
        /// </summary>
        /// <param name="polyline">给定的长度</param>
        /// <param name="esriUnit">地图单位</param>
        /// <param name="unitDescript">单位中文描述</param>
        /// <returns></returns>
        public double ConvertLengthToMapDis(double length, esriUnits esriUnit, out string unitDescriptor)
        {
            unitDescriptor = "";
 
            switch (esriUnit)
            {
                // "未知单位";
                case esriUnits.esriUnknownUnits:
                    unitDescriptor = "未知单位"; break;
                // "英寸";
                case esriUnits.esriInches:
                    length = length / 0.3048 * 1000;
                    unitDescriptor = "英寸"; break;
                //"像素点";
                case esriUnits.esriPoints:
                    length = length / 0.3528 * 1000 * 1000;
                    unitDescriptor = "像素点"; break;
                // "英尺";
                case esriUnits.esriFeet:
                    length = length / 0.3048 * 1000;
                    unitDescriptor = "英尺"; break;
                // "码";
                case esriUnits.esriYards:
                    length = length / 0.9144 * 1000;
                    unitDescriptor = "码"; break;
                // "英里";
                case esriUnits.esriMiles:
                    length = length / 1609.344 * 1000;
                    unitDescriptor = "英里"; break;
                //"海里";
                case esriUnits.esriNauticalMiles:
                    length = length / 1852 * 1000;
                    unitDescriptor = "海里"; break;
                // "毫米";
                case esriUnits.esriMillimeters:
                    length = length * 1000 * 1000;
                    unitDescriptor = "毫米"; break;
                // "厘米";
                case esriUnits.esriCentimeters:
                    length = length / 100 / 1000;
                    unitDescriptor = "厘米"; break;
                // "公里";
                case esriUnits.esriKilometers:
                    unitDescriptor = "公里"; break;
                // "十进制度";
                case esriUnits.esriDecimalDegrees:
                    length = length / 112;//这里采用近似方法处理1度=112km【实际上也可以换算】
                    unitDescriptor = "十进制度"; break;
                //"分米";
                case esriUnits.esriDecimeters:
                    length = length * 10 * 1000;
                    unitDescriptor = "分米"; break;
                //"米"
                case esriUnits.esriMeters:
                    length = length * 1000;
                    unitDescriptor = "米"; break;
            }
 
            return length;
        }

          /// <summary>
        /// 获取球面折线的长度，单位米
        /// </summary>
        /// <param name="pGeometry"></param>
        /// <returns>如果错误，返回-1；否则返回折线的长度</returns>
        public static double Sphere_GetPolylineLength(IGeometry pGeometry)
        {
            if (pGeometry is IPolyline)
            {
                IPointCollection pointCollection = pGeometry as IPointCollection;
                if (pointCollection == null) return -1;

                double totalDistance = 0;
                for (int i = 0; i < pointCollection.PointCount - 1; i++)
                {
                    IPoint pt1 = pointCollection.get_Point(i);
                    IPoint pt2 = pointCollection.get_Point(i + 1);

                    totalDistance += Sphere_DistanceOfTwoPoints(pt1.X, pt1.Y, pt2.X, pt2.Y);
                }

                return totalDistance;
            }

            return -1;
        }



        /// <summary>
        /// 计算两个给定经纬度的点在球上的距离
        /// </summary>
        /// <param name="lng1"></param>
        /// <param name="lat1"></param>
        /// <param name="lng2"></param>
        /// <param name="lat2"></param>
        /// <returns></returns>
         private static double Sphere_DistanceOfTwoPoints(double lng1, double lat1, double lng2, double lat2) {
            double radLat1 = Rad(lat1);
            double radLat2 = Rad(lat2);
            double a = radLat1 - radLat2;
            double b = Rad(lng1) - Rad(lng2);
            double s = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) +
                Math.Cos(radLat1) * Math.Cos(radLat2) * Math.Pow(Math.Sin(b / 2), 2)));
            s = s * 6378137.0; //以wgs84椭球为例
            s = Math.Round(s * 10000) / 10000;
            return s;
        }
        
        /// <summary>
        /// 角度转换
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
         private static double Rad(double d)
         {
             return d * Math.PI / 180.0;
         }

        /// <summary>
        /// 深拷贝
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
        /// 添加FlowPath与destination重叠的约束条件（移除非TarGrid中其它格点k阶邻近范围内的点）未考虑重叠和节点间疏密的约束
        /// </summary>
        /// <param name="desGrids">desPoint的格网</param>
        /// <param name="WeighGrids">权重格网</param>
        /// <param name="k">删除的限制数量</param>K=1表示自身；=2表示2阶邻近
        /// <param name="j">当前格网编码</param>
        public void FlowOverLayContraint(List<Tuple<int, int>> desGrids,Dictionary<Tuple<int, int>, double> WeighGrids, int k, Tuple<int,int> TaretDes)
        {
            for (int n = 0; n < desGrids.Count; n++)
            {
                if (desGrids[n].Item1 != TaretDes.Item1 || desGrids[n].Item2 != TaretDes.Item2)
                {
                    List<Tuple<int, int>> NearGrids = Fs.GetNearGrids(desGrids[n], WeighGrids.Keys.ToList(), k);

                    foreach (Tuple<int, int> Grid in NearGrids)
                    {
                        if (WeighGrids.Keys.Contains(Grid))
                        {
                            WeighGrids.Remove(Grid);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 添加FlowPath与destination重叠的约束条件（移除非TarGrid中其它格点k阶邻近范围内的点）考虑重叠和节点间疏密的约束!!!
        /// </summary>
        /// <param name="desGrids">desPoint的格网</param>
        /// <param name="WeighGrids">权重格网</param>
        /// <param name="k">删除的限制数量</param>K=1表示自身；=2表示2阶邻近
        /// <param name="j">当前格网编码</param>
        public void FlowOverLayContraint_2(List<Tuple<int, int>> desGrids, Dictionary<Tuple<int, int>, double> WeighGrids, int k, Tuple<int, int> TaretDes)
        {
            for (int n = 0; n < desGrids.Count; n++)
            {
                if (desGrids[n].Item1 != TaretDes.Item1 && desGrids[n].Item2 != TaretDes.Item2)
                {
                    List<Tuple<int, int>> NearGrids = Fs.GetNearGrids_2(desGrids,desGrids[n], WeighGrids.Keys.ToList(), k);

                    foreach (Tuple<int, int> Grid in NearGrids)
                    {
                        if (WeighGrids.Keys.Contains(Grid))
                        {
                            WeighGrids.Remove(Grid);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 添加FlowPath与destination重叠的约束条件（移除非TarGrid中其它格点k阶邻近范围内的点）考虑重叠和节点间疏密的约束!!!
        /// </summary>
        /// <param name="desGrids">desPoint的格网</param>
        /// <param name="WeighGrids">权重格网</param>
        /// <param name="k">删除的限制数量</param>K=1表示自身；=2表示2阶邻近
        /// <param name="j">当前格网编码</param>
        public void FlowOverLayContraint_3(List<Tuple<int, int>> desGrids, Dictionary<Tuple<int, int>, IPoint> GridWithNode, Dictionary<Tuple<int, int>, double> WeighGrids, int k, Tuple<int, int> TaretDes, Dictionary<Tuple<int, int>, List<double>> GridValue)
        {
            for (int n = 0; n < desGrids.Count; n++)
            {
                if (desGrids[n].Item1 != TaretDes.Item1 || desGrids[n].Item2 != TaretDes.Item2)
                {
                    List<Tuple<int, int>> NearGrids = Fs.GetNearGrids_3(desGrids, desGrids[n], GridWithNode, WeighGrids.Keys.ToList(), GridValue,k);

                    foreach (Tuple<int, int> Grid in NearGrids)
                    {
                        if (WeighGrids.Keys.Contains(Grid))
                        {
                            WeighGrids.Remove(Grid);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 添加FlowPath与destination重叠的约束条件【移除PathGrid】
        /// </summary>
        /// <param name="WeighGrids">权重格网</param>
        /// <param name="k">删除的限制数量</param>K=1表示自身；=2表示2阶邻近
        /// <param name="j">当前格网编码</param>
        public void FlowCrosssingContraint(Dictionary<Tuple<int, int>, double> WeighGrids, int k, Tuple<int, int> TaretDes,Tuple<int,int> OriginGrid,List<Tuple<int,int>> PathGrids)
        {
            #region 移除PathGrids
            for (int n = 0; n < PathGrids.Count; n++)
            {
                if (PathGrids[n].Item1 != OriginGrid.Item1 || PathGrids[n].Item2 != OriginGrid.Item2)
                {
                    List<Tuple<int, int>> NearGrids = Fs.GetNearGrids(PathGrids[n], WeighGrids.Keys.ToList(), 0);

                    foreach (Tuple<int, int> Grid in NearGrids)
                    {
                        if (WeighGrids.ContainsKey(Grid))
                        {
                            WeighGrids.Remove(Grid);
                        }
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// 获得给定的所有des节点在不同方向下的搜索图(只考虑了0阶的Overlay约束)
        /// </summary>
        /// <param name="pWeighGrids"></param>
        /// <param name="desGrids"></param>
        /// <returns></returns>
        public Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> GetDesDirPt(Dictionary<Tuple<int, int>, double> pWeighGrids, List<Tuple<int, int>> desGrids)
        {
            Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> DesDirPt = new Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>>();

            foreach (Tuple<int, int> Grid in desGrids)
            {
                Dictionary<int, PathTrace> CacheDirPt = this.GetDirPt(pWeighGrids, Grid, desGrids, Grid);
                if (!DesDirPt.ContainsKey(Grid))
                {
                    DesDirPt.Add(Grid, CacheDirPt);
                }
            }


            return DesDirPt;
        }

        /// <summary>
        /// 获得给定的所有des节点在不同方向下的搜索图(考虑n阶Overlay约束)
        /// </summary>
        /// <param name="pWeighGrids"></param>
        /// <param name="desGrids"></param>
        /// <returns></returns>
        public Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> GetDesDirPt_2(Dictionary<Tuple<int, int>, double> pWeighGrids, List<Tuple<int, int>> desGrids,int k)
        {
            Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> DesDirPt = new Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>>();

            foreach (Tuple<int, int> Grid in desGrids)
            {
                Dictionary<int, PathTrace> CacheDirPt = this.GetDirPt_2(pWeighGrids, Grid, desGrids, Grid, k);
                if (!DesDirPt.ContainsKey(Grid))
                {
                    DesDirPt.Add(Grid, CacheDirPt);
                }
            }

            return DesDirPt;
        }

        /// <summary>
        /// 获得给定的所有des节点在不同方向下的搜索图
        /// </summary>
        /// <param name="pWeighGrids"></param>
        /// <param name="desGrids"></param>
        /// <returns></returns>
        public Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> GetDesDirPtDis(Dictionary<Tuple<int, int>, double> pWeighGrids, List<Tuple<int, int>> desGrids, Dictionary<Tuple<int, int>, int> GridType)
        {
            Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> DesDirPt = new Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>>();


            foreach (Tuple<int, int> Grid in desGrids)
            {
                Dictionary<int, PathTrace> CacheDirPt = this.GetDirPtDis(pWeighGrids, Grid, desGrids, Grid, GridType);
                DesDirPt.Add(Grid, CacheDirPt);
            }

            return DesDirPt;
        }

        /// <summary>
        /// 获得给定节点不同约束方向下的搜索图1（只考虑了0阶的Overlay约束）
        /// </summary>
        /// <param name="WeighGrids">权重格网</param>
        /// <param name="Grid">目标网格</param>
        /// <param name="desGrids">所有目标格网</param>
        /// <param name="i">当前格网编号</param>
        /// <returns></returns>获取给定节点不同方向编码的路径搜索
        public Dictionary<int, PathTrace> GetDirPt(Dictionary<Tuple<int, int>, double> pWeighGrids, Tuple<int, int> Grid, List<Tuple<int, int>> desGrids,Tuple<int,int> TargetDes)
        {          
            Dictionary<int, PathTrace> DirPt = new Dictionary<int, PathTrace>();
            for (int n = 0; n < 9; n++)
            {
                #region 获取DirList
                List<int> DirList = new List<int>();
                if (n >= 4 && n <= 6)
                {
                    DirList.Add(n);
                    DirList.Add(n + 1);
                    DirList.Add(n + 2);
                }

                else if (n >= 1 && n <= 3)
                {
                    DirList.Add(n + 2);
                    DirList.Add(n + 1);
                    DirList.Add(n);
                }

                else if (n == 0)
                {
                    DirList.Add(2);
                    DirList.Add(1);
                    DirList.Add(8);
                }

                else if (n == 7)
                {
                    DirList.Add(7);
                    DirList.Add(8);
                    DirList.Add(1);
                }

                else if (n == 8)
                {
                    DirList.Add(1);
                    DirList.Add(2);
                    DirList.Add(3);
                    DirList.Add(4);
                    DirList.Add(5);
                    DirList.Add(6);
                    DirList.Add(7);
                    DirList.Add(8);
                }
                #endregion

                Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝
                this.FlowOverLayContraint(desGrids, WeighGrids, 0, TargetDes);//Overlay约束
                PathTrace Pt = new PathTrace();
                List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                JudgeList.Add(Grid);//添加搜索的起点
                Pt.MazeAlg(JudgeList, WeighGrids, 1, DirList);//备注：每次更新以后,WeightGrid会清零  

                int Number = this.GetNumber(DirList);
                DirPt.Add(Number, Pt);
            }

            return DirPt;
        }

        /// <summary>
        /// 获得给定节点不同约束方向下的搜索图1（考虑n阶的Overlay约束）
        /// </summary>
        /// <param name="WeighGrids">权重格网</param>
        /// <param name="Grid">目标网格</param>
        /// <param name="desGrids">所有目标格网</param>
        /// <param name="i">当前格网编号</param>
        /// <returns></returns>获取给定节点不同方向编码的路径搜索
        public Dictionary<int, PathTrace> GetDirPt_2(Dictionary<Tuple<int, int>, double> pWeighGrids, Tuple<int, int> Grid, List<Tuple<int, int>> desGrids, Tuple<int, int> TargetDes,int k)
        {
            Dictionary<int, PathTrace> DirPt = new Dictionary<int, PathTrace>();
            for (int n = 0; n < 9; n++)
            {
                #region 获取DirList
                List<int> DirList = new List<int>();
                if (n >= 4 && n <= 6)
                {
                    DirList.Add(n);
                    DirList.Add(n + 1);
                    DirList.Add(n + 2);
                }

                else if (n >= 1 && n <= 3)
                {
                    DirList.Add(n + 2);
                    DirList.Add(n + 1);
                    DirList.Add(n);
                }

                else if (n == 0)
                {
                    DirList.Add(2);
                    DirList.Add(1);
                    DirList.Add(8);
                }

                else if (n == 7)
                {
                    DirList.Add(7);
                    DirList.Add(8);
                    DirList.Add(1);
                }

                else if (n == 8)
                {
                    DirList.Add(1);
                    DirList.Add(2);
                    DirList.Add(3);
                    DirList.Add(4);
                    DirList.Add(5);
                    DirList.Add(6);
                    DirList.Add(7);
                    DirList.Add(8);
                }
                #endregion

                Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝
                this.FlowOverLayContraint_2(desGrids, WeighGrids, k, TargetDes);//Overlay约束
                PathTrace Pt = new PathTrace();
                List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                JudgeList.Add(Grid);//添加搜索的起点
                Pt.MazeAlg(JudgeList, WeighGrids, 1, DirList);//备注：每次更新以后,WeightGrid会清零  

                int Number = this.GetNumber(DirList);
                DirPt.Add(Number, Pt);
            }

            return DirPt;
        }

        /// <summary>
        /// 获得给定节点不同约束方向下的搜索图1[考虑网格距离差异]
        /// </summary>
        /// <param name="WeighGrids">权重格网</param>
        /// <param name="Grid">目标网格</param>
        /// <param name="desGrids">所有目标格网</param>
        /// <param name="i">当前格网编号</param>
        /// <returns></returns>获取给定节点不同方向编码的路径搜索
        public Dictionary<int, PathTrace> GetDirPtDis(Dictionary<Tuple<int, int>, double> pWeighGrids, Tuple<int, int> Grid, List<Tuple<int, int>> desGrids, Tuple<int, int> TargetDes, Dictionary<Tuple<int, int>, int> GridType)
        {
            Dictionary<int, PathTrace> DirPt = new Dictionary<int, PathTrace>();
            for (int n = 0; n < 9; n++)
            {
                #region 获取DirList
                List<int> DirList = new List<int>();
                if (n >= 4 && n <= 6)
                {
                    DirList.Add(n);
                    DirList.Add(n + 1);
                    DirList.Add(n + 2);
                }

                else if (n >= 1 && n <= 3)
                {
                    DirList.Add(n + 2);
                    DirList.Add(n + 1);
                    DirList.Add(n);
                }

                else if (n == 0)
                {
                    DirList.Add(2);
                    DirList.Add(1);
                    DirList.Add(8);
                }

                else if (n == 7)
                {
                    DirList.Add(7);
                    DirList.Add(8);
                    DirList.Add(1);
                }

                else if (n == 8)
                {
                    DirList.Add(1);
                    DirList.Add(2);
                    DirList.Add(3);
                    DirList.Add(4);
                    DirList.Add(5);
                    DirList.Add(6);
                    DirList.Add(7);
                    DirList.Add(8);
                }
                #endregion

                Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝
                Dictionary<Tuple<int, int>, int> pGridVisit = Clone((object)GridType) as Dictionary<Tuple<int, int>, int>;//深拷贝

                this.FlowOverLayContraint(desGrids, WeighGrids, 0, TargetDes);//Overlay约束
                PathTrace Pt = new PathTrace();
                List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                JudgeList.Add(Grid);//添加搜索的起点
                Pt.MazeAlgDis(JudgeList, WeighGrids, 1, DirList,GridType,pGridVisit);//备注：每次更新以后,WeightGrid会清零  

                int Number = this.GetNumber(DirList);
                DirPt.Add(Number, Pt);
            }

            return DirPt;
        }

        /// <summary>
        /// 获取给定路径的长度
        /// </summary>
        /// <param name="ShortestPath"></param>
        /// <returns></returns>
        public double GetPathLength(List<Tuple<int, int>> ShortestPath)
        {
            double Length = 0;

            for (int i = 0; i < ShortestPath.Count - 1; i++)
            {
                if (ShortestPath[i].Item1 == ShortestPath[i + 1].Item1 ||
                   ShortestPath[i].Item2 == ShortestPath[i + 1].Item2)
                {
                    Length = Length + 1;
                }

                else
                {
                    Length = Length + Math.Sqrt(2);
                }
            }

            return Length;
        }

        /// <summary>
        /// 获取给定路径的长度
        /// </summary>
        /// <param name="ShortestPath"></param>
        /// <returns></returns>
        public double GetPathLengthDis(List<Tuple<int, int>> ShortestPath,Dictionary<Tuple<int,int>,int> GridType)
        {
            double Length = 0;

            for (int i = 0; i < ShortestPath.Count - 1; i++)
            {
                if (ShortestPath[i].Item1 == ShortestPath[i + 1].Item1 ||
                   ShortestPath[i].Item2 == ShortestPath[i + 1].Item2)
                {
                    Length = (GridType[ShortestPath[i]] + 1) / 2 + (GridType[ShortestPath[i + 1]] + 1) / 2 + Length;
                }

                else
                {
                    Length = Length + (GridType[ShortestPath[i]] + 1) / 2 * Math.Sqrt(2) + (GridType[ShortestPath[i + 1]] + 1) / 2 * Math.Sqrt(2);
                }
            }

            return Length;
        }

        /// <summary>
        /// 获取给定路径的长度(类型对换)
        /// </summary>
        /// <param name="ShortestPath"></param>
        /// <returns></returns>
        public double GetPathLengthDisRever(List<Tuple<int, int>> ShortestPath, Dictionary<Tuple<int, int>, int> GridType)
        {
            double Length = 0;

            for (int i = 0; i < ShortestPath.Count - 1; i++)
            {
                if (ShortestPath[i].Item1 == ShortestPath[i + 1].Item1 ||
                   ShortestPath[i].Item2 == ShortestPath[i + 1].Item2)
                {
                    Length = (Math.Abs(GridType[ShortestPath[i]]-10) + 1) / 2 + (Math.Abs(GridType[ShortestPath[i + 1]]-10) + 1) / 2 + Length;
                }

                else
                {
                    Length = Length + (Math.Abs(GridType[ShortestPath[i]]-10) + 1) / 2 * Math.Sqrt(2) + (Math.Abs(GridType[ShortestPath[i + 1]]-10) + 1) / 2 * Math.Sqrt(2);
                }
            }

            return Length;
        }

        /// <summary>
        /// 判断PathGrid是否能作为DesGrid的潜在FlowInlocations
        /// 判断应该是作为同一侧的点才有可能是FlowInLocation
        /// </summary>
        /// <param name="DesGrid">终点</param>
        /// <param name="PathGrid">流入点</param>
        /// <param name="StartGrid">起点</param>
        /// <returns></returns>
        public bool JudgeGrid(Tuple<int, int> DesGrid, Tuple<int, int> PathGrid, Tuple<int, int> StartGrid)
        {
            #region 判断起点在终点的哪一侧
            int DSI = DesGrid.Item1 - StartGrid.Item1;
            int DSJ = DesGrid.Item2 - StartGrid.Item2;

            int PSI = PathGrid.Item1 - StartGrid.Item1;
            int PSJ = PathGrid.Item2 - StartGrid.Item2;
            #endregion

            if (DSI * PSI >= 0 && PSJ * DSJ >= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 判断PathGrid是否能作为DesGrid的潜在FlowInlocations
        /// 判断应该是作为同一侧的点才有可能是FlowInLocation
        /// </summary>
        /// <param name="DesGrid">终点</param>
        /// <param name="PathGrid">流入点</param>
        /// <param name="StartGrid">起点</param>
        /// <returns></returns>
        public bool JudgeGrid2(Tuple<int, int> DesGrid, Tuple<int, int> PathGrid, Tuple<int, int> StartGrid)
        {
            #region 判断起点在终点的哪一侧
            int MinI = Math.Min(DesGrid.Item1, StartGrid.Item1);
            int MaxI = Math.Max(DesGrid.Item1, StartGrid.Item1);
            int MinJ = Math.Min(DesGrid.Item2, StartGrid.Item2);
            int MaxJ = Math.Max(DesGrid.Item2, StartGrid.Item2);
            #endregion

            if (PathGrid.Item1 >= MinI && PathGrid.Item1 <= MaxI
                && PathGrid.Item2 >= MinJ && PathGrid.Item2 <= MaxJ)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 判断Grid是否与直线在同一侧
        /// </summary>
        /// <param name="DesGrid"></param>
        /// <param name="PathGrid"></param>
        /// <param name="StartGrid"></param>
        /// <returns></returns>
        public bool RLJudgeGrid(Tuple<int, int> DesGrid, Tuple<int, int> PathGrid, Tuple<int, int> StartGrid)
        {
            if (this.JudgeGrid2(DesGrid, PathGrid, StartGrid))
            {
                return true;
            }
            

            double DP=Math.Sqrt((PathGrid.Item1-DesGrid.Item1)*(PathGrid.Item1-DesGrid.Item1)+(PathGrid.Item2-DesGrid.Item2)*(PathGrid.Item2-DesGrid.Item2));
            double DS=Math.Sqrt((StartGrid.Item1-DesGrid.Item1)*(StartGrid.Item1-DesGrid.Item1)+(StartGrid.Item2-DesGrid.Item2)*(StartGrid.Item2-DesGrid.Item2));
            double PS=Math.Sqrt((PathGrid.Item1-StartGrid.Item1)*(PathGrid.Item1-StartGrid.Item1)+(PathGrid.Item2-StartGrid.Item2)*(PathGrid.Item2-StartGrid.Item2));

            double CosA = (DS * DS + DP * DP - PS * PS) / (2 * DS * DP);
            if (CosA >= 0)
            {
                return true;
            }

            else
            {
                return false;
            }
            
        }


        /// <summary>
        /// 判断ePoint向sPoint延伸的限制性方向
        /// </summary>
        /// <param name="sPoint"></param>
        /// <param name="ePoint"></param>
        /// <returns></returns>
        public List<int> GetConDir(Tuple<int, int> sPoint, Tuple<int, int> ePoint)
        {
            List<int> DirList = new List<int>();//获取限定的方向列表

            #region 获取限定方向(方向编码：1-8,1正下，顺时针编码)
            int IADD = sPoint.Item1 - ePoint.Item1;
            int JADD = sPoint.Item2 - ePoint.Item2;

            if (IADD == 0)
            {
                if (JADD > 0)
                {
                    DirList.Add(4); DirList.Add(5); DirList.Add(6);
                }

                if (JADD < 0)
                {
                    DirList.Add(2); DirList.Add(1); DirList.Add(8);
                }
            }

            else if (IADD > 0)
            {
                if (JADD > 0)
                {
                    DirList.Add(5); DirList.Add(6); DirList.Add(7);
                }

                if (JADD == 0)
                {
                    DirList.Add(6); DirList.Add(7); DirList.Add(8);
                }

                if (JADD < 0)
                {
                    DirList.Add(7); DirList.Add(8); DirList.Add(1);
                }
            }

            else if (IADD < 0)
            {
                if (JADD > 0)
                {
                    DirList.Add(5); DirList.Add(4); DirList.Add(3);
                }

                if (JADD == 0)
                {
                    DirList.Add(4); DirList.Add(3); DirList.Add(2);
                }

                if (JADD < 0)
                {
                    DirList.Add(3); DirList.Add(2); DirList.Add(1);
                }
            }
            #endregion

            //DirList.Sort();
            return DirList;
        }

        /// <summary>
        /// 判断ePoint向sPoint延伸的限制性方向
        /// </summary>
        /// <param name="sPoint"></param>
        /// <param name="ePoint"></param>
        /// <returns></returns>
        public List<int> GetConDirR(Tuple<int, int> sPoint, Tuple<int, int> ePoint)
        {
            List<int> DirList = new List<int>();//获取限定的方向列表

            #region 获取限定方向(方向编码：1-8,1正下，顺时针编码)
            int IADD = sPoint.Item1 - ePoint.Item1;
            int JADD = sPoint.Item2 - ePoint.Item2;

            if (IADD == 0)
            {
                if (JADD > 0)
                {
                    DirList.Add(6); DirList.Add(7); DirList.Add(8);
                }

                if (JADD < 0)
                {
                    DirList.Add(4); DirList.Add(3); DirList.Add(2);
                }
            }

            else if (IADD > 0)
            {
                if (JADD > 0)
                {
                    DirList.Add(5); DirList.Add(6); DirList.Add(7);
                }

                if (JADD == 0)
                {
                    DirList.Add(4); DirList.Add(5); DirList.Add(6);
                }

                if (JADD < 0)
                {
                    DirList.Add(5); DirList.Add(4); DirList.Add(3);
                }
            }

            else if (IADD < 0)
            {
                if (JADD > 0)
                {
                    DirList.Add(7); DirList.Add(8); DirList.Add(1);
                }

                if (JADD == 0)
                {
                    DirList.Add(2); DirList.Add(1); DirList.Add(8);
                }

                if (JADD < 0)
                {
                    DirList.Add(3); DirList.Add(2); DirList.Add(1);
                }
            }
            #endregion

            //DirList.Sort();
            return DirList;
        }

        /// <summary>
        /// 判断ePoint向sPoint延伸的限制性方向
        /// </summary>
        /// <param name="sPoint"></param>
        /// <param name="ePoint"></param>
        /// <returns></returns>
        public List<int> GetConDir2(Tuple<int, int> sPoint, Tuple<int, int> ePoint)
        {
            List<int> DirList = new List<int>();//获取限定的方向列表

            #region 获取限定方向(方向编码：1-8,1正下，顺时针编码)
            int IADD = sPoint.Item1 - ePoint.Item1;
            int JADD = sPoint.Item2 - ePoint.Item2;

            #region JADD不等于0
            if (IADD != 0)
            {
                double Tan = JADD / IADD;
                double Angle = Math.Atan(Tan);

                #region IADD大于0
                if (IADD > 0)
                {
                    #region JADD大于0
                    if (JADD > 0)
                    {

                        if (Angle < Math.PI / 8)
                        {
                            DirList.Add(6); DirList.Add(7); DirList.Add(8);
                        }

                        else if (Angle < Math.PI / 8 * 3)
                        {
                            DirList.Add(5); DirList.Add(6); DirList.Add(7);
                        }

                        else
                        {
                            DirList.Add(4); DirList.Add(5); DirList.Add(6);
                        }
                    }
                    #endregion

                    #region JADD小于0
                    else
                    {
                        if (Math.Abs(Angle) < Math.PI / 8)
                        {
                            DirList.Add(6); DirList.Add(7); DirList.Add(8);
                        }

                        else if (Math.Abs(Angle) < Math.PI / 8 * 3)
                        {
                            DirList.Add(7); DirList.Add(8); DirList.Add(1);
                        }

                        else
                        {
                            DirList.Add(2); DirList.Add(1); DirList.Add(8);
                        }
                    }
                    #endregion
                }
                #endregion

                #region IDD小于0
                else
                {
                    #region JADD大于0
                    if (JADD > 0)
                    {
                        if (Math.Abs(Angle) < Math.PI / 8)
                        {
                            DirList.Add(4); DirList.Add(3); DirList.Add(2);
                        }

                        else if (Math.Abs(Angle) < Math.PI / 8 * 3)
                        {
                            DirList.Add(5); DirList.Add(4); DirList.Add(3);
                        }

                        else
                        {
                            DirList.Add(4); DirList.Add(5); DirList.Add(6);
                        }
                    }
                    #endregion

                    #region JADD小于0
                    else
                    {
                        if (Angle < Math.PI / 8)
                        {
                            DirList.Add(4); DirList.Add(3); DirList.Add(2);
                        }

                        else if (Angle < Math.PI / 8 * 3)
                        {
                            DirList.Add(3); DirList.Add(2); DirList.Add(1);
                        }

                        else
                        {
                            DirList.Add(2); DirList.Add(1); DirList.Add(8);
                        }
                    }
                    #endregion
                }
                #endregion
            
            }
            #endregion

            #region IADD==0
            else
            {
                if (JADD > 0)
                {
                    DirList.Add(6); DirList.Add(7); DirList.Add(8);
                }

                else
                {
                    DirList.Add(4); DirList.Add(3); DirList.Add(2);
                }
            }
            #endregion

            #endregion
            //DirList.Sort();
            return DirList;
        }

        /// <summary>
        /// 将List<int>转换成一个唯一标识的数字
        /// </summary>
        /// <param name="DirList"></param>
        /// <returns></returns>
        public int GetNumber(List<int> DirList)
        {
            int OutNumber = 0;

            for (int i = 0; i < DirList.Count; i++)
            {
                OutNumber = OutNumber + DirList[i] * (int)Math.Pow(10, i);
            }

            return OutNumber;
        }

        /// <summary>
        /// 判断新生成的路径是否相交
        /// </summary>
        /// <param name="CachePath">给定路径</param>
        /// <param name="PathGrids">已生成的路径Grids</param>
        /// <returns>=true表示相交；=false表示不相交</returns>
        public bool IntersectPath(List<Tuple<int, int>> CacheShortPath, List<Tuple<int, int>> PathGrids)
        {
            int IntersectCount = 0;

            foreach (Tuple<int, int> TaGrid in CacheShortPath)
            {
                if (this.GridContain(TaGrid,PathGrids))
                {
                    IntersectCount++;
                }
            }

            if (IntersectCount >= 2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 判断新生成的路径是否相交
        /// </summary>
        /// <param name="CachePath">给定路径</param>
        /// <param name="PathGrids">已生成的路径Grids</param>
        /// <returns>=true表示相交；=false表示不相交</returns>
        public int IntersectPathInt(List<Tuple<int, int>> CacheShortPath, List<Tuple<int, int>> PathGrids)
        {
            int IntersectCount = 0;
            bool IntersectLabel = false;

             if (this.GridContain(CacheShortPath[0], PathGrids))
             {
                  IntersectCount++;
             }

             for (int i = 1; i < CacheShortPath.Count; i++)
             {
                 if (this.GridContain(CacheShortPath[i], PathGrids))
                 {
                     IntersectCount++;
                 }

                 if (this.GridContain(CacheShortPath[i - 1], PathGrids) && this.GridContain(CacheShortPath[i], PathGrids))
                 {
                     return 1;
                 }

                 Tuple<int, int> CacheGrid1 = new Tuple<int, int>(CacheShortPath[i - 1].Item1, CacheShortPath[i].Item2);
                 Tuple<int, int> CacheGrid2 = new Tuple<int, int>(CacheShortPath[i].Item1, CacheShortPath[i-1].Item2);
                 if (this.GridContain(CacheGrid1, PathGrids) && this.GridContain(CacheGrid2, PathGrids))
                 {
                     IntersectLabel = true;
                 }

             }         

            if (IntersectCount >= 2||IntersectLabel)
            {
                return 2;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// 计算给定DesGrid到PathGrids的直线距离（不考虑搜索路径）
        /// </summary>
        /// <param name="DesGrid"></param>
        /// <param name="PathGrids"></param>
        /// <returns></returns>
        public double GetMinDis(Tuple<int, int> DesGrid, List<Tuple<int, int>> PathGrids)
        {
            double MinDis = 1000000;

            foreach (Tuple<int, int> Grid in PathGrids)
            {
                double IADD = Math.Abs(Grid.Item1 - DesGrid.Item1);
                double JADD = Math.Abs(Grid.Item2 - DesGrid.Item2);

                double Dis = Math.Sqrt(IADD * IADD + JADD * JADD);
                if (Dis < MinDis)
                {
                    MinDis = Dis;
                }
            }

            return MinDis;
        }

        /// <summary>
        /// 计算给定网格到起点的距离
        /// </summary>
        /// <param name="DesGrid"></param>
        /// <param name="StartOrder"></param>
        /// <returns></returns>
        public Dictionary<Tuple<int,int>,double> GetDisOrder(List<Tuple<int,int>> DesGrids,Tuple<int,int> StartGrid)
        {
            Dictionary<Tuple<int, int>, double> GridDis = new Dictionary<Tuple<int, int>, double>();

            foreach (Tuple<int, int> Grid in DesGrids)
            {
                double IADD = Grid.Item1 - StartGrid.Item1;
                double JADD = Grid.Item2 - StartGrid.Item2;

                double Dis = Math.Sqrt(IADD * IADD + JADD * JADD);
                GridDis.Add(Grid, Dis);
            }

            return GridDis;
        }

        /// <summary>
        /// 判断新生成的路径是否相交
        /// </summary>
        /// <param name="CachePath">给定路径</param>
        /// <param name="PathGrids">已生成的路径Grids</param>
        /// <returns>=true表示相交；=false表示不相交</returns>
        public bool LineIntersectPath(List<Tuple<int, int>> CacheShortPath, List<Tuple<int, int>> PathGrids, Dictionary<Tuple<int, int>, List<double>> Grids)
        {
            object missing = Type.Missing;

            #region CacheShortPath
            IGeometry shp1 = new PolylineClass();
            IPointCollection pointSet1 = shp1 as IPointCollection;
            IPoint curResultPoint1 = new PointClass();
            if (CacheShortPath != null)
            {
                for (int i = 0; i < CacheShortPath.Count; i++)
                {
                    double X = (Grids[CacheShortPath[i]][0] + Grids[CacheShortPath[i]][2]) / 2;
                    double Y = (Grids[CacheShortPath[i]][1] + Grids[CacheShortPath[i]][3]) / 2;

                    curResultPoint1.PutCoords(X, Y);
                    pointSet1.AddPoint(curResultPoint1, ref missing, ref missing);
                }
            }
            #endregion

            #region PathGrids
            IGeometry shp2= new PolylineClass();
            IPointCollection pointSet2 = shp2 as IPointCollection;
            IPoint curResultPoint2 = new PointClass();
            if (PathGrids != null)
            {
                for (int i = 0; i < PathGrids.Count; i++)
                {
                    double X = (Grids[PathGrids[i]][0] + Grids[PathGrids[i]][2]) / 2;
                    double Y = (Grids[PathGrids[i]][1] + Grids[PathGrids[i]][3]) / 2;

                    curResultPoint2.PutCoords(X, Y);
                    pointSet2.AddPoint(curResultPoint2, ref missing, ref missing);
                }
            }
            #endregion

            #region 判断过程
            ITopologicalOperator iTop = shp2 as ITopologicalOperator;
            IGeometry IGeo1 = iTop.Intersect(shp1 as IGeometry,esriGeometryDimension.esriGeometry0Dimension);
            IGeometry IGeo2 = iTop.Intersect(shp1 as IGeometry, esriGeometryDimension.esriGeometry1Dimension);

            if (IGeo1 != null)
            {
                IPointCollection IPC = IGeo1 as IPointCollection;
                if (IPC.PointCount >= 2)
                {
                    return true;
                }

                else
                {
                    return false;
                }
            }

            if (IGeo2 != null)
            {
                IPointCollection IPC = IGeo2 as IPointCollection;
                if (IPC.PointCount >= 2)
                {
                    return true;
                }

                else
                {
                    return false;
                }
            }
            #endregion

            return false;
        }

        /// <summary>
        /// 判断生成路径是否与Features相交
        /// </summary>
        /// <param name="CacheShortPath"></param>
        /// <param name="Features"></param>
        /// <returns></returns>
        public bool obstacleIntersectPath(List<Tuple<int, int>> CacheShortPath, List<Tuple<IGeometry, esriGeometryType>> Features, Dictionary<Tuple<int, int>, List<double>> Grids)
        {
            bool obstacleIntersect = false;
           
            #region CacheShortPath
            object missing = Type.Missing;
            IGeometry shp1 = new PolylineClass();
            IPointCollection pointSet1 = shp1 as IPointCollection;
            IPoint curResultPoint1 = new PointClass();
            if (CacheShortPath != null)
            {
                for (int i = 0; i < CacheShortPath.Count; i++)
                {
                    double X = (Grids[CacheShortPath[i]][0] + Grids[CacheShortPath[i]][2]) / 2;
                    double Y = (Grids[CacheShortPath[i]][1] + Grids[CacheShortPath[i]][3]) / 2;

                    curResultPoint1.PutCoords(X, Y);
                    pointSet1.AddPoint(curResultPoint1, ref missing, ref missing);
                }
            }
            #endregion

            #region 判断相交（点目标无需判断）
            foreach(Tuple<IGeometry,esriGeometryType> Feature in Features)
            {
                #region 线状目标
                if (Feature.Item2 == esriGeometryType.esriGeometryPolyline)
                {
                    ITopologicalOperator iTo = Feature.Item1 as ITopologicalOperator;
                    IGeometry IGeo = iTo.Intersect(shp1, esriGeometryDimension.esriGeometry0Dimension);
                    if (!IGeo.IsEmpty)
                    {
                        return true;
                    }
                }
                #endregion

                #region 面状目标
                if (Feature.Item2 == esriGeometryType.esriGeometryPolygon)
                {
                    ITopologicalOperator iTo = Feature.Item1 as ITopologicalOperator;
                    IGeometry IGeo = iTo.Intersect(shp1, esriGeometryDimension.esriGeometry1Dimension);
                    if (!IGeo.IsEmpty)
                    {
                        return true;
                    }
                }
                #endregion
            }
            #endregion

            return obstacleIntersect;
        }

        /// <summary>
        /// 判断是否包含某一个Grid
        /// </summary>
        /// <param name="Grid"></param>
        /// <param name="PathGrids"></param>
        /// <returns></returns>
        public bool GridContain(Tuple<int, int> Grid, List<Tuple<int, int>> PathGrids)
        {
            foreach (Tuple<int, int> CacheGrid in PathGrids)
            {
                if (CacheGrid.Item1 == Grid.Item1 &&
                    CacheGrid.Item2 == Grid.Item2)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
