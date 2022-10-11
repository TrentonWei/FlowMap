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

using AuxStructureLib;
using AuxStructureLib.IO;

namespace PrDispalce.FlowMap
{
    class PublicUtil
    {
        /// <summary>
        /// 计算建筑物图形上某节点处的角度
        /// </summary>
        /// <param name="TriNode1"></param>
        /// <param name="TriNode2"></param>
        /// <param name="TriNode3"></param>
        /// <returns></returns>
        public double GetPointAngle(TriNode CurPoint, TriNode BeforePoint, TriNode AfterPoint)
        {
            #region 计算叉积信息
            double Vector1X = BeforePoint.X - CurPoint.X; double Vector1Y = BeforePoint.Y - CurPoint.Y;
            double Vector2X = AfterPoint.X - CurPoint.X; double Vector2Y = AfterPoint.Y - CurPoint.Y;

            double xMultiply = Vector1X * Vector2Y - Vector1Y * Vector2X;//获得叉积信息，用于判断顺逆时针
            #endregion

            #region 计算角度信息(顺时针角度为正；逆时针角度为负)
            double Angle = this.GetAngle(CurPoint, AfterPoint, BeforePoint);
            if (xMultiply < 0)
            {
                Angle = Angle * (-1);
            }
            #endregion

            return Angle;
        }

        /// <summary>
        /// 获得沿边左侧或右侧偏移指定距离的点
        /// </summary>
        /// <param name="sPoint">起点</param>
        /// <param name="ePoint">终点</param>
        /// TarPoint 指定需偏移的点
        /// <param name="ShiftDis">偏移距离</param>
        /// <param name="shift">偏移方向</param>=1向左偏移；=3向右偏移
        /// <returns></returns>
        public void GetShiftPoint(IPoint sPoint, IPoint ePoint, IPoint TarPoint, double ShiftDis, int shift)
        {
            double Dis = Math.Sqrt((ePoint.X - sPoint.X) * (ePoint.X - sPoint.X) + (ePoint.Y - sPoint.Y) * (ePoint.Y - sPoint.Y));
            double cos = (ePoint.X - sPoint.X) / Dis; double sin = (ePoint.Y - sPoint.Y) / Dis;

            #region 向左偏移
            if (shift == 1)
            {
                TarPoint.X = TarPoint.X - sin * ShiftDis;
                TarPoint.Y = TarPoint.Y + cos * ShiftDis;
            }
            #endregion

            #region 向右偏移
            if (shift == 3)
            {
                TarPoint.X = TarPoint.X + sin * ShiftDis;
                TarPoint.Y = TarPoint.Y - cos * ShiftDis; ;
            }
            #endregion
        }

        /// <summary>
        /// 获得线要素的点列表
        /// </summary>
        /// <param name="PL"></param>
        /// <returns></returns>
        public List<IPoint> GetPoints(PolylineObject PL)
        {
            List<IPoint> Points = new List<IPoint>();
            foreach (TriNode Tn in PL.PointList)
            {
                IPoint pPoint = new PointClass();
                pPoint.X = Tn.X;
                pPoint.Y = Tn.Y;

                Points.Add(pPoint);
            }

            return Points;
        }

        /// <summary>
        /// 获得沿边左侧或右侧偏移指定距离的点
        /// </summary>
        /// <param name="sPoint">起点</param>
        /// <param name="ePoint">终点</param>
        /// TarPoint 指定需偏移的点
        /// <param name="ShiftDis">偏移距离</param> 若>0，向左偏移；若<0，向右偏移
        /// <returns></returns>
        public void GetShiftPoint(IPoint sPoint, IPoint ePoint, IPoint TarPoint, double ShiftDis)
        {
            double Dis = Math.Sqrt((ePoint.X - sPoint.X) * (ePoint.X - sPoint.X) + (ePoint.Y - sPoint.Y) * (ePoint.Y - sPoint.Y));
            double cos = (ePoint.X - sPoint.X) / Dis; double sin = (ePoint.Y - sPoint.Y) / Dis;

            TarPoint.X = TarPoint.X - sin * ShiftDis;
            TarPoint.Y = TarPoint.Y + cos * ShiftDis;
        }

        /// <summary>
        /// 获得线段的定比分点
        /// </summary>
        /// sn:ne=rate 若n在se外，rate是负值
        /// <param name="sPoint"></param>
        /// <param name="ePoint"></param>
        /// <param name="Rate"></param>
        /// <returns></returns>nPoint
        public IPoint GetExtendPoint(IPoint sPoint, IPoint ePoint, double Rate)
        {
            double x = (sPoint.X + Rate * ePoint.X) / (1 + Rate);
            double y = (sPoint.Y + Rate * ePoint.Y) / (1 + Rate);
            IPoint nPoint = new PointClass();
            nPoint.X = x; nPoint.Y = y;
            return nPoint;
        }

        /// <summary>
        /// 给定直线上两点，获取从给定点出发与该直线平行的平行线
        /// </summary>
        /// <param name="CurPoint"></param>
        /// <param name="ParaNode1"></param>
        /// <param name="ParaNode2"></param>
        /// <returns></returns>
        public IPolyline GetParaLine(TriNode CurPoint, TriNode ParaNode1, TriNode ParaNode2)
        {
            IPolyline ParaLine = new PolylineClass();

            double k = (ParaNode2.Y - ParaNode1.Y) / (ParaNode2.X - ParaNode1.X);

            if (ParaNode2.X - ParaNode1.X != 0)
            {
                double x1 = CurPoint.X - 1000; double y1 = CurPoint.Y - 1000 * k;
                double x2 = CurPoint.X + 1000; double y2 = CurPoint.Y + 1000 * k;
                IPoint FromPoint = new PointClass(); FromPoint.X = x1; FromPoint.Y = y1;
                IPoint ToPoint = new PointClass(); ToPoint.X = x2; ToPoint.Y = y2;
                ParaLine.FromPoint = FromPoint; ParaLine.ToPoint = ToPoint;
            }

            else
            {
                double x1 = CurPoint.X; double y1 = CurPoint.Y - 1000;
                double x2 = CurPoint.X; double y2 = CurPoint.Y + 1000;
                IPoint FromPoint = new PointClass(); FromPoint.X = x1; FromPoint.Y = y1;
                IPoint ToPoint = new PointClass(); ToPoint.X = x2; ToPoint.Y = y2;
                ParaLine.FromPoint = FromPoint; ParaLine.ToPoint = ToPoint;
            }

            return ParaLine;
        }

        /// <summary>
        /// 给定两点，获得CurPoint沿該条直线的延长线
        /// </summary>
        /// <param name="CurPoint"></param>
        /// <param name="EndPoint"></param>
        /// <returns></returns>
        public IPolyline GetExtendingLine(TriNode CurPoint, TriNode EndPoint)
        {
            IPolyline ExtendingLine = new PolylineClass();

            IPoint StartNode = new PointClass(); StartNode.X = CurPoint.X; StartNode.Y = CurPoint.Y;
            IPoint EndNode = new PointClass();
            double k = (EndPoint.Y - CurPoint.Y) / (EndPoint.X - CurPoint.X);
            if (CurPoint.X > EndPoint.X)
            {
                double X = CurPoint.X + 1000;
                double Y = CurPoint.Y + 1000 * k;
                EndNode.X = X; EndNode.Y = Y;
            }

            else if (CurPoint.X == EndPoint.X)
            {
                double X = CurPoint.X;
                double Y = CurPoint.Y + 1000;
                EndNode.X = X; EndNode.Y = Y;
            }

            else
            {
                double X = CurPoint.X - 1000;
                double Y = CurPoint.Y - 1000 * k;
                EndNode.X = X; EndNode.Y = Y;
            }

            ExtendingLine.FromPoint = StartNode;
            ExtendingLine.ToPoint = EndNode;

            return ExtendingLine;
        }

        /// <summary>
        /// 给定三点，计算该点的角度值(0,PI)
        /// </summary>
        /// <param name="curNode"></param>
        /// <param name="TriNode1"></param> AfterPoint后节点！
        /// <param name="TriNode2"></param> BeforePoint前节点！
        /// <returns></returns>
        public double GetAngle(TriNode curNode, TriNode TriNode1, TriNode TriNode2)
        {
            double a = Math.Sqrt((curNode.X - TriNode1.X) * (curNode.X - TriNode1.X) + (curNode.Y - TriNode1.Y) * (curNode.Y - TriNode1.Y));
            double b = Math.Sqrt((curNode.X - TriNode2.X) * (curNode.X - TriNode2.X) + (curNode.Y - TriNode2.Y) * (curNode.Y - TriNode2.Y));
            double c = Math.Sqrt((TriNode1.X - TriNode2.X) * (TriNode1.X - TriNode2.X) + (TriNode1.Y - TriNode2.Y) * (TriNode1.Y - TriNode2.Y));

            double CosCur = (a * a + b * b - c * c) / (2 * a * b);
            double Angle = Math.Acos(CosCur);

            return Angle;
        }

        /// <summary>
        /// 给定三点，计算该点的角度值(0,PI)
        /// </summary>
        /// <param name="curNode"></param>
        /// <param name="TriNode1"></param> AfterPoint后节点！
        /// <param name="TriNode2"></param> BeforePoint前节点！
        /// <returns></returns>
        public double GetAngle(IPoint curNode, IPoint TriNode1, IPoint TriNode2)
        {
            TriNode tcurNode = new TriNode(curNode.X, curNode.Y);
            TriNode tTriNode1 = new TriNode(TriNode1.X, TriNode1.Y);
            TriNode tTriNode2 = new TriNode(TriNode2.X, TriNode2.Y);
            return this.GetAngle(tcurNode, tTriNode1, tTriNode2);
        }

        /// <summary>
        /// 将建筑物转化为IPolygon
        /// </summary>
        /// <param name="pPolygonObject"></param>
        /// <returns></returns>
        public IPolygon PolygonObjectConvert(PolygonObject pPolygonObject)
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
        /// polygon转换成polygonobject
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <returns></returns>
        public PolygonObject PolygonConvert(IPolygon pPolygon)
        {
            int ppID = 0;//（polygonobject自己的编号，应该无用）
            List<TriNode> trilist = new List<TriNode>();
            //Polygon的点集
            IPointCollection pointSet = pPolygon as IPointCollection;
            int count = pointSet.PointCount;
            double curX;
            double curY;
            //ArcGIS中，多边形的首尾点重复存储
            for (int i = 0; i < count - 1; i++)
            {
                curX = pointSet.get_Point(i).X;
                curY = pointSet.get_Point(i).Y;
                //初始化每个点对象
                TriNode tPoint = new TriNode(curX, curY, ppID, 1);
                trilist.Add(tPoint);
            }
            //生成自己写的多边形
            PolygonObject mPolygonObject = new PolygonObject(ppID, trilist);

            return mPolygonObject;
        }

        /// <summary>
        /// 获得旋转后的多边形
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <param name="Orientation"></param>
        /// <returns></returns>
        public IPolygon GetRotatedPolygon(IPolygon pPolygon, double Orientation)
        {
            IArea pArea = pPolygon as IArea;
            IPoint CenterPoint = pArea.Centroid;
            ITransform2D pTransform2D = pPolygon as ITransform2D;
            pTransform2D.Rotate(CenterPoint, Orientation);
            return pTransform2D as IPolygon;
        }

        /// <summary>
        /// 获得平移后的多边形
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <param name="pPoint"></param>
        /// <returns></returns>
        public IPolygon GetPannedPolygon(IPolygon pPolygon, IPoint pPoint)
        {
            IArea pArea = pPolygon as IArea;
            IPoint CenterPoint = pArea.Centroid;

            double Dx = pPoint.X - CenterPoint.X;
            double Dy = pPoint.Y - CenterPoint.Y;

            ITransform2D pTransform2D = pPolygon as ITransform2D;
            pTransform2D.Move(Dx, Dy);
            return pTransform2D as IPolygon;
        }

        /// <summary>
        /// 获得放大后的多边形
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <param name="EnlargeRate"></param>
        /// <returns></returns>
        public IPolygon GetEnlargedPolygon(IPolygon pPolygon, double EnlargeRate)
        {
            IArea pArea = pPolygon as IArea;
            IPoint CenterPoint = pArea.Centroid;

            ITransform2D pTransform2D = pPolygon as ITransform2D;
            pTransform2D.Scale(CenterPoint, EnlargeRate, EnlargeRate);
            return pTransform2D as IPolygon;
        }

        /// <summary>
        /// 将建筑物缩放至与TargetPo面积一致
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <param name="EnlargeRate"></param>
        /// <returns></returns>
        public IPolygon GetEnlargedPolygon(IPolygon pPolygon, IPolygon TargetPo)
        {
            IArea tArea = TargetPo as IArea;
            double tA = tArea.Area;

            IArea pArea = pPolygon as IArea;
            IPoint CenterPoint = pArea.Centroid;
            double pA = pArea.Area;

            double EnlargeRate = pA / tA;

            ITransform2D pTransform2D = pPolygon as ITransform2D;
            pTransform2D.Scale(CenterPoint, EnlargeRate, EnlargeRate);
            return pTransform2D as IPolygon;
        }

        /// <summary>
        /// 获取给定Feature的属性
        /// </summary>
        /// <param name="CurFeature"></param>
        /// <param name="FieldString"></param>
        /// <returns></returns>
        public double GetValue(IFeature curFeature, string FieldString)
        {
            double Value = 0;

            IFields pFields = curFeature.Fields;
            int field1 = pFields.FindField(FieldString);
            Value = Convert.ToDouble(curFeature.get_Value(field1));

            return Value;
        }

        /// <summary>
        /// 获取给定Feature的属性
        /// </summary>
        /// <param name="CurFeature"></param>
        /// <param name="FieldString"></param>
        /// <returns></returns>
        public int GetintValue(IFeature curFeature, string FieldString)
        {
            int Value = 0;

            IFields pFields = curFeature.Fields;
            int field1 = pFields.FindField(FieldString);
            Value = Convert.ToInt32(curFeature.get_Value(field1));

            return Value;
        }

        /// <summary>
        /// 获得给定的两点间的距离
        /// </summary>
        /// <param name="sPoint"></param>
        /// <param name="ePoint"></param>
        /// <returns></returns>
        public double GetDis(IPoint sPoint, IPoint ePoint)
        {
            return Math.Sqrt((ePoint.Y - sPoint.Y) * (ePoint.Y - sPoint.Y) + (ePoint.X - sPoint.X) * (ePoint.X - sPoint.X));
        }

        /// <summary>
        /// 获得给定的两点间的距离
        /// </summary>
        /// <param name="sPoint"></param>
        /// <param name="ePoint"></param>
        /// <returns></returns>
        public double GetDis(TriNode sPoint, TriNode ePoint)
        {
            return Math.Sqrt((ePoint.Y - sPoint.Y) * (ePoint.Y - sPoint.Y) + (ePoint.X - sPoint.X) * (ePoint.X - sPoint.X));
        }

        /// <summary>
        /// 计算给定线的光滑程度
        /// </summary>
        /// <returns></returns>
        public double GetSmoothValue(PolylineObject PLine)
        {
            double Smooth = 0;double PI=3.1415926;
            PublicUtil Pu=new PublicUtil();
            double DisE = Pu.GetDis(PLine.PointList[0], PLine.PointList[PLine.PointList.Count - 1]);
            double DisI = 0;

            for (int i = 1; i < PLine.PointList.Count-1; i++)
            {
                double Angle = this.GetAngle(PLine.PointList[i], PLine.PointList[i + 1], PLine.PointList[i - 1]);
                DisI = DisI + Pu.GetDis(PLine.PointList[i], PLine.PointList[i - 1]) * Math.Abs(Math.Sin(PI - Angle));
            }

            Smooth = 1 - DisI / DisE;
            return Smooth;
        }
    }
}
