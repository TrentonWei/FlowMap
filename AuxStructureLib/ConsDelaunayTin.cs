﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;

namespace AuxStructureLib
{
    public class ConsDelaunayTin
    {
        public DelaunayTin DT = null;

        public ConsDelaunayTin(DelaunayTin dt)
        {
            this.DT = dt;
        }

        /// <summary>
        /// 三角形列表
        /// </summary>
        public List<Triangle> TriangleList
        {
            get
            {
                if (this.DT == null)
                    return null;
                else
                    return this.DT.TriangleList;
            }
        }

        /// <summary>
        /// 删除仍在polygon中的三角形
        /// </summary>
        public void RemoveTriangleInPolygon(List<PolygonObject> PolygonList)
        {
            for (int i = this.DT.TriangleList.Count - 1; i >= 0; i--)
            {
                Triangle Tri = this.DT.TriangleList[i];
                foreach (PolygonObject Po in PolygonList)
                {
                    if (Po.PointList.Contains(Tri.point1))
                    {
                        if (Po.PointList.Contains(Tri.point2))
                        {
                            if (Po.PointList.Contains(Tri.point3))
                            {
                                this.DT.TriangleList.RemoveAt(i);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 边列表
        /// </summary>
        public List<TriEdge> TriEdgeList
        {
            get
            {
                if (this.DT == null)
                    return null;
                else
                    return this.DT.TriEdgeList;
            }
        }

        /// <summary>
        /// 顶点列表
        /// </summary>
        public List<TriNode> TriNodeList
        {
            get
            {
                if (this.DT == null)
                    return null;
                else
                    return this.DT.TriNodeList;
            }
        }
        /// <summary>
        /// 创建约束CDT
        /// </summary>
        /// <param name="polylineList">约束线</param>
        /// <param name="polygonList">约束面</param>
        public void CreateConsDTfromPolylineandPolygon(List<PolylineObject> polylineList, List<PolygonObject> polygonList)
        {
            if (polylineList != null)//线
            {
                foreach (PolylineObject curPolyline in polylineList)
                {
                    //无约束边
                    if (curPolyline == null || curPolyline.PointList.Count < 2)
                    {
                        return;
                    }

                    List<TriNode> ConstPointList = curPolyline.PointList;//线上的点集合
                    int tagID = curPolyline.ID;//线目标的ID
                    int n = ConstPointList.Count;
                    List<TriEdge> edgeList = this.DT.TriEdgeList;
                    List<Triangle> triList = this.DT.TriangleList;

                    for (int i = 0; i < n - 1; i++)
                    {
                        TriNode sp = ConstPointList[i];//起点
                        TriNode ep = ConstPointList[i + 1];//终点

                        //插入一条约束边
                        InsertConstEdge(sp, ep, triList, tagID,FeatureType.PolylineType);
                    }
                }
            }

            if (polygonList != null)//面
            {
                foreach (PolygonObject curPolygon in polygonList)
                {
                    //无约束边
                    if (curPolygon == null || curPolygon.PointList.Count < 3)
                    {
                        return;
                    }

                    List<TriNode> ConstPointList = curPolygon.PointList;//线上的点集合
                    int tagID = curPolygon.ID;//面目标的ID
                    int n = ConstPointList.Count;
                    List<TriEdge> edgeList = this.DT.TriEdgeList;
                    List<Triangle> triList = this.DT.TriangleList;

                    for (int i = 0; i < n; i++)
                    {
                        TriNode sp = null;//起点
                        TriNode ep = null;//终点
                        if (i == n - 1)
                        {
                            sp = ConstPointList[i];//起点
                            ep = ConstPointList[0];//终点
                            if (sp == ep)
                                continue;
                        }
                        else
                        {
                            sp = ConstPointList[i];//起点
                            ep = ConstPointList[i + 1];//终点
                            if (sp == ep)
                                continue;
                        }

                        //插入一条约束边
                        InsertConstEdge(sp, ep, triList, tagID,FeatureType.PolygonType);

                    }
                }
            }

            //重建边列表==？？可能并不需要
            Triangle.WriteID(DT.TriangleList);
            DT.TriEdgeList = Triangle.GetEdges(DT.TriangleList);
            TriEdge.WriteID(DT.TriEdgeList);
            TriEdge.AmendEdgeLeftTriangle(DT.TriEdgeList);
            //测试
            //Triangle.Create_WriteTriange2Shp(@"E:\DelaunayShape", @"ConsTri", tl, esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
        }
        /// <summary>
        /// 重新组织三角网中的边关系
        /// </summary>
        public void Refresh()
        {
            Triangle.WriteID(DT.TriangleList);
            DT.TriEdgeList = Triangle.GetEdges(DT.TriangleList);
            TriEdge.WriteID(DT.TriEdgeList);
            TriEdge.AmendEdgeLeftTriangle(DT.TriEdgeList);
        }


        /// <summary>
        /// 创建约束CDT
        /// </summary>
        public void CreateConstDTfromPolylines(List<PolylineObject> polylineList)
        {
            foreach (PolylineObject curPolyline in polylineList)
            {
                //无约束边
                if (curPolyline == null || curPolyline.PointList.Count < 2)
                {
                    return;
                }

                List<TriNode> ConstPointList = curPolyline.PointList;//线上的点集合
                int tagID = curPolyline.ID;//线目标的ID
                int n = ConstPointList.Count;
                List<TriEdge> edgeList = this.DT.TriEdgeList;
                List<Triangle> triList = this.DT.TriangleList;

                for (int i = 0; i < n - 1; i++)
                {
                    TriNode sp = ConstPointList[i];//起点
                    TriNode ep = ConstPointList[i + 1];//终点

                    //插入一条约束边
                    InsertConstEdge(sp, ep, triList, tagID,FeatureType.PolylineType);
                }

            }

            //重建边列表==？？可能并不需要
            Triangle.WriteID(DT.TriangleList);
            DT.TriEdgeList = Triangle.GetEdges(DT.TriangleList);
            TriEdge.WriteID(DT.TriEdgeList);
            TriEdge.AmendEdgeLeftTriangle(DT.TriEdgeList);
            //测试
            //Triangle.Create_WriteTriange2Shp(@"E:\DelaunayShape", @"ConsTri", tl, esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
        }

        /// <summary>
        /// 从三角网中提取影响范围
        /// </summary>
        /// <param name="triList">三角网</param>
        /// <param name="sp">约束边起点</param>
        /// <param name="ep">约束边终点</param>
        /// <param name="consTriList">与约束边相交的三角形集合</param>
        /// <param name="intersectEdgeList">提取的相交边集合</param>
        /// <returns>是否与三角网中的三角形相交</returns>
        private bool FineIntersectList(List<Triangle> triList, TriNode sp, TriNode ep, int tagID, out  List<Triangle> consTriList, out List<TriEdge> intersectEdgeList, FeatureType type)
        {
            consTriList = null;    //影响区域三角形
            intersectEdgeList = null; //相交的
            foreach (Triangle tri in triList)
            {
                if (tri.IsContainPoint(sp))
                {
                    #region 给定的边是三角形的一条边
                    if (tri.IsContainPoint(ep))
                    {
                        //===设置约束边标识
                        TriEdge TriCE = null;
                        if ((tri.edge1.startPoint.ID == sp.ID && tri.edge1.endPoint.ID == ep.ID) || (tri.edge1.endPoint.ID == sp.ID && tri.edge1.startPoint.ID == ep.ID))
                        {
                            TriCE = tri.edge1;
                        }
                        else if ((tri.edge2.startPoint.ID == sp.ID && tri.edge2.endPoint.ID == ep.ID) || (tri.edge2.endPoint.ID == sp.ID && tri.edge2.startPoint.ID == ep.ID))
                        {
                            TriCE = tri.edge2;
                        }
                        else if ((tri.edge3.startPoint.ID == sp.ID && tri.edge3.endPoint.ID == ep.ID) || (tri.edge3.endPoint.ID == sp.ID && tri.edge3.startPoint.ID == ep.ID))
                        {
                            TriCE = tri.edge3;
                        }
                        if (TriCE != null)
                        {
                            //if (TriCE.ptagID[0] == -1)
                            //    TriCE.ptagID[0] = tagID;
                            //else
                            //    TriCE.ptagID[1] = tagID;

                            //if (TriCE.doulEdge != null)
                            //{
                            //    if (TriCE.doulEdge.ptagID[0] == -1)
                            //        TriCE.doulEdge.ptagID[0] = tagID;
                            //    else
                            //        TriCE.doulEdge.ptagID[1] = tagID;
                            //}

                            TriCE.tagID = tagID;
                            TriCE.FeatureType = type;
                            if (TriCE.doulEdge != null)
                            {
                                TriCE.doulEdge.tagID = tagID;
                                TriCE.doulEdge.FeatureType = type;
                            }
                        }
                        return false;
                    }
                    #endregion

                    #region
                    else
                    {
                        #region 找到对边
                        TriEdge curE = null;
                        if (!tri.edge1.IsContainPoint(sp))
                        {
                            curE = tri.edge1;
                        }
                        else if (!tri.edge2.IsContainPoint(sp))
                        {
                            curE = tri.edge2;
                        }
                        else if (!tri.edge3.IsContainPoint(sp))
                        {
                            curE = tri.edge3;
                        }
                        #endregion

                        #region 判段是否相交
                        if (curE != null)
                        {

                            if (ComFunLib.IsLineSegCross(sp, ep, curE.startPoint, curE.endPoint))
                            {
                                consTriList = new List<Triangle>();
                                //if (!consTriList.Contains(tri))
                                //{
                                consTriList.Add(tri);
                                //}
                                intersectEdgeList = new List<TriEdge>();
                                intersectEdgeList.Add(curE);

                                Triangle nextTri = null;
                                TriEdge nextE1 = null;
                                TriEdge nextE2 = null;

                                while ((nextTri = curE.rightTriangle) != null)
                                {
                                    if (consTriList.Count > 10000)
                                    {
                                        break;
                                    }
                                    //if (!consTriList.Contains(nextTri))
                                    //{
                                    consTriList.Add(nextTri);
                                    //}

                                    if (nextTri.IsContainPoint(ep))
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        if (((nextTri.edge1.startPoint == curE.startPoint) && (nextTri.edge1.endPoint == curE.endPoint)) || ((nextTri.edge1.startPoint == curE.endPoint) && (nextTri.edge1.endPoint == curE.startPoint)))
                                        {
                                            nextE1 = nextTri.edge2;
                                            nextE2 = nextTri.edge3;
                                        }
                                        else if (((nextTri.edge2.startPoint == curE.startPoint) && (nextTri.edge2.endPoint == curE.endPoint)) || ((nextTri.edge2.startPoint == curE.endPoint) && (nextTri.edge2.endPoint == curE.startPoint)))
                                        {
                                            nextE1 = nextTri.edge1;
                                            nextE2 = nextTri.edge3;
                                        }
                                        else if (((nextTri.edge3.startPoint == curE.startPoint) && (nextTri.edge3.endPoint == curE.endPoint)) || ((nextTri.edge3.startPoint == curE.endPoint) && (nextTri.edge3.endPoint == curE.startPoint)))
                                        {
                                            nextE1 = nextTri.edge1;
                                            nextE2 = nextTri.edge2;
                                        }

                                        if (ComFunLib.IsLineSegCross(sp, ep, nextE1.startPoint, nextE1.endPoint))
                                        {
                                            curE = nextE1;
                                            intersectEdgeList.Add(curE);
                                        }
                                        else if (ComFunLib.IsLineSegCross(sp, ep, nextE2.startPoint, nextE2.endPoint))
                                        {
                                            curE = nextE2;
                                            intersectEdgeList.Add(curE);
                                        }

                                    }
                                }
                                return true;
                            }
                        }
                        #endregion

                        if (consTriList != null)
                        {
                            if (consTriList.Count > 10000)
                            {
                                break;
                            }
                        }
                    }
                    #endregion
                }
            }
            return false;
        }

        /// <summary>
        /// 向三角网中插入约束边
        /// </summary>
        /// <param name="sp">起点</param>
        /// <param name="ep">终点</param>
        /// <param name="triList">三角网</param>
        /// <param name="tagID">代码</param>
        private void InsertConstEdge(TriNode sp, TriNode ep, List<Triangle> triList, int tagID, FeatureType featureType)
        {
            List<Triangle> consTriList = new List<Triangle>();
            List<TriEdge> intersectEdgeList = new List<TriEdge>();

            //判断是否与三角形剖分相交
            if (FineIntersectList(triList, sp, ep, tagID, out  consTriList, out intersectEdgeList, featureType))
            {
                int j = 0;
                while (intersectEdgeList.Count > 0 && j < intersectEdgeList.Count)
                {
                    TriEdge curEdge = intersectEdgeList[j];
                    Triangle LTri = curEdge.leftTriangle;
                    Triangle RTri = curEdge.rightTriangle;
                    TriNode LPoint = null;
                    TriNode RPoint = null;
                    TriEdge LTEdge = null;
                    TriEdge LBEdge = null;
                    TriEdge RTEdge = null;
                    TriEdge RBEdge = null;

                    #region 另外一个对角线
                    if (RTri != null && LTri != null)
                    {
                        if ((LTri.point1.ID != curEdge.startPoint.ID) && (LTri.point1.ID != curEdge.endPoint.ID))
                        {
                            LPoint = LTri.point1;
                        }
                        else if ((LTri.point2.ID != curEdge.startPoint.ID) && (LTri.point2.ID != curEdge.endPoint.ID))
                        {
                            LPoint = LTri.point2;
                        }
                        else if ((LTri.point3.ID != curEdge.startPoint.ID) && (LTri.point3.ID != curEdge.endPoint.ID))
                        {
                            LPoint = LTri.point3;
                        }

                        if (LTri.edge1.startPoint.ID == LPoint.ID || LTri.edge1.endPoint.ID == LPoint.ID)
                        {
                            if (LTri.edge1.startPoint.ID == LPoint.ID)
                                LBEdge = LTri.edge1;
                            else
                                LTEdge = LTri.edge1;
                        }
                        if (LTri.edge2.startPoint.ID == LPoint.ID || LTri.edge2.endPoint.ID == LPoint.ID)
                        {
                            if (LTri.edge2.startPoint.ID == LPoint.ID)
                                LBEdge = LTri.edge2;
                            else
                                LTEdge = LTri.edge2;
                        }
                        if (LTri.edge3.startPoint.ID == LPoint.ID || LTri.edge3.endPoint.ID == LPoint.ID)
                        {
                            if (LTri.edge3.startPoint.ID == LPoint.ID)
                                LBEdge = LTri.edge3;
                            else
                                LTEdge = LTri.edge3;
                        }


                        if ((RTri.point1.ID != curEdge.startPoint.ID) && (RTri.point1.ID != curEdge.endPoint.ID))
                        {
                            RPoint = RTri.point1;
                        }
                        else if ((RTri.point2.ID != curEdge.startPoint.ID) && (RTri.point2.ID != curEdge.endPoint.ID))
                        {
                            RPoint = RTri.point2;
                        }
                        else if ((RTri.point3.ID != curEdge.startPoint.ID) && (RTri.point3.ID != curEdge.endPoint.ID))
                        {
                            RPoint = RTri.point3;
                        }

                        if (RTri.edge1.startPoint.ID == RPoint.ID || RTri.edge1.endPoint.ID == RPoint.ID)
                        {
                            if (RTri.edge1.startPoint.ID == RPoint.ID)
                                RTEdge = RTri.edge1;
                            else
                                RBEdge = RTri.edge1;
                        }
                        if (RTri.edge2.startPoint.ID == RPoint.ID || RTri.edge2.endPoint.ID == RPoint.ID)
                        {
                            if (RTri.edge2.startPoint.ID == RPoint.ID)
                                RTEdge = RTri.edge2;
                            else
                                RBEdge = RTri.edge2;
                        }
                        if (RTri.edge3.startPoint.ID == RPoint.ID || RTri.edge3.endPoint.ID == RPoint.ID)
                        {
                            if (RTri.edge3.startPoint.ID == RPoint.ID)
                                RTEdge = RTri.edge3;
                            else
                                RBEdge = RTri.edge3;
                        }
                    }

                    #endregion

                    #region 如果相交则交换对边且不与约束边相交
                    if (ComFunLib.IsLineSegCross(LPoint, RPoint, curEdge.startPoint, curEdge.endPoint)/*&&!ComFunLib.IsLineSegCross(LPoint,RPoint, sp, ep)*/)
                    {
                        if (RTri != null && LTri != null)
                        {
                            Triangle TopTri = new Triangle();
                            Triangle BotTri = new Triangle();

                            TopTri.point1 = LPoint; TopTri.point2 = RPoint; TopTri.point3 = curEdge.endPoint;
                            TopTri.edge1 = new TriEdge(LPoint, RPoint); TopTri.edge2 = RTEdge; TopTri.edge3 = LTEdge;

                            TopTri.edge1.leftTriangle = TopTri;
                            TopTri.edge1.rightTriangle = BotTri;

                            TopTri.edge2.leftTriangle = TopTri; TopTri.edge3.leftTriangle = TopTri;

                            // TriEdge resLEdge2 = TriEdge.FindOppsiteEdge(edgeList, TopTri.edge2);
                            TriEdge resLEdge2 = TopTri.edge2.doulEdge;
                            if (resLEdge2 != null)
                                resLEdge2.rightTriangle = TopTri;
                            //  TriEdge resLEdge3 = TriEdge.FindOppsiteEdge(edgeList, TopTri.edge3);
                            TriEdge resLEdge3 = TopTri.edge3.doulEdge;
                            if (resLEdge3 != null)
                                resLEdge3.rightTriangle = TopTri;

                            BotTri.point1 = RPoint; BotTri.point2 = LPoint; BotTri.point3 = curEdge.startPoint;
                            BotTri.edge1 = new TriEdge(RPoint, LPoint); BotTri.edge2 = LBEdge; BotTri.edge3 = RBEdge;
                            BotTri.edge1.leftTriangle = BotTri;
                            BotTri.edge1.rightTriangle = TopTri;

                            BotTri.edge2.leftTriangle = BotTri; BotTri.edge3.leftTriangle = BotTri;

                            //TriEdge resREdge2 = TriEdge.FindOppsiteEdge(edgeList, BotTri.edge2);
                            TriEdge resREdge2 = BotTri.edge2.doulEdge;
                            if (resREdge2 != null)
                            {
                                resREdge2.rightTriangle = BotTri;
                            }
                            //TriEdge resEdge3 = TriEdge.FindOppsiteEdge(edgeList, BotTri.edge3);
                            TriEdge resEdge3 = BotTri.edge3.doulEdge;
                            if (resEdge3 != null)
                                resEdge3.rightTriangle = BotTri;

                            TopTri.edge1.doulEdge = BotTri.edge1;
                            BotTri.edge1.doulEdge = TopTri.edge1;

                            if (LPoint.ID == sp.ID && RPoint.ID == ep.ID)
                            {
                                //if (TopTri.edge1.ptagID[0] == -1)
                                //    TopTri.edge1.ptagID[0] = tagID;
                                //else
                                //    TopTri.edge1.ptagID[1] = tagID;

                                //if (BotTri.edge1.ptagID[0] == -1)
                                //    BotTri.edge1.ptagID[0] = tagID;
                                //else
                                //    BotTri.edge1.ptagID[1] = tagID;

                                TopTri.edge1.tagID = tagID;
                                TopTri.edge1.FeatureType = featureType;

                                BotTri.edge1.tagID = tagID;
                                BotTri.edge1.FeatureType = featureType;
                            }

                            consTriList.Remove(LTri);
                            consTriList.Remove(RTri);

                            triList.Remove(LTri);
                            triList.Remove(RTri);

                            consTriList.Add(TopTri);
                            consTriList.Add(BotTri);

                            triList.Add(TopTri);
                            triList.Add(BotTri);

                            intersectEdgeList.Remove(curEdge);

                            if (ComFunLib.IsLineSegCross(LPoint, RPoint, sp, ep))
                            {
                                intersectEdgeList.Add(TopTri.edge1);
                            }
                            j = 0;
                            continue;
                        }

                    }
                    #endregion

                    j++;
                }
            }
        }
    }
}
