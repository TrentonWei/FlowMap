using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using System.IO;

namespace AuxStructureLib
{
    /// <summary>
    /// 构建Delaunay算法类型
    /// </summary>
    public enum AlgDelaunayType
    {
        Side_extent, 
        Point_insert, 
        Divide_conquer,
        ESRI_AE,
        Side_extent2
    }
    /// <summary>
    /// Delaunay三角网
    /// </summary>
    public class DelaunayTin
    {
        public List<Triangle> TriangleList = null;  //三角形列表
        public List<TriEdge> TriEdgeList = null;    //边列表
        public List<TriNode> TriNodeList = null;    //节点列表
        private SMap map = null;
        /// <summary>
        /// 三角形个数
        /// </summary>
        public int TriangleCount
        {
            get
            {
                return TriangleList.Count;
            }
        }

        /// <summary>
        /// 边数
        /// </summary>
        public int TriNodeCount
        {
            get
            {
                return TriNodeList.Count;
            }
        }

        /// <summary>
        /// 节点数
        /// </summary>
        public int TriEdgeCount
        {
            get
            {
                return TriEdgeList.Count;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="triNodeList"></param>
        public DelaunayTin(List<TriNode> triNodeList)
        {
            TriNodeList = triNodeList;
            TriangleList = new List<Triangle>();
            TriEdgeList = new List<TriEdge>();
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="triNodeList"></param>
        public DelaunayTin(SMap map)
        {
            this.map = map;
            this.TriNodeList = map.TriNodeList;
            TriangleList = new List<Triangle>();
            TriEdgeList = new List<TriEdge>();
        }



        //List<TriPoint> triPoint = new List<TriPoint>();
       // List<TriEdge> triEdgeTemp = new List<TriEdge>();
       // List<Triangle> listTriangle = new List<Triangle>();
       // List<PointF> circumPoints = new List<PointF>();
        /// <summary>
        /// 创建Delaunay三角网
        /// </summary>
        /// <param name="algType">算法类型</param>
        public void CreateDelaunayTin(AlgDelaunayType algType)
        {
            switch (algType)
            {
                case AlgDelaunayType.Side_extent:
                    CreateDelaunayTin_Side_extent();
                    break;
                case AlgDelaunayType.Side_extent2:
                    CreateDelaunayTin_Side_extent2();
                    break;
                case AlgDelaunayType.Point_insert:
                    break;
                case  AlgDelaunayType.Divide_conquer:
                    break;
                //case AlgDelaunayType.ESRI_AE:
                //    this.CreateDelaunayAE();
                //    break;
            }
        }

        public void CreateDelaunayTin(AlgDelaunayType algType, string TinPath)
        {
            this.CreateDelaunayAE(TinPath);
        }

        public void DeleteTriangle(Triangle tri)
        {
            TriEdge e1 = tri.edge1;
            TriEdge e2 = tri.edge2;
            TriEdge e3 = tri.edge3;

            if (e1 != null)
            {
                TriEdge de = e1.doulEdge;
                if (de != null)
                {
                    de.doulEdge = null;
                    de.rightTriangle = null;
                }
                TriEdgeList.Remove(e1);
            }
            if (e2 != null)
            {
                TriEdge de = e2.doulEdge;
                if (de != null)
                {
                    de.doulEdge = null;
                    de.rightTriangle = null;
                }
                TriEdgeList.Remove(e2);
            }
            if (e3 != null)
            {
                TriEdge de = e3.doulEdge;
                if (de != null)
                {
                    de.doulEdge = null;
                    de.rightTriangle = null;
                }
                TriEdgeList.Remove(e3);
            }
            TriangleList.Remove(tri);
        }


        #region 调用AE生成三角网
        /// <summary>
        /// 调用AE生成三角网
        /// </summary>
        private void CreateDelaunayAE(string TinPath)
        {
            // Instantiate a new empty TIN.
            ITinEdit TinEdit = new TinClass();

            // Initialize the TIN with an envelope. The envelope's extent should be set large enough to // encompass all the data that will be added to the TIN. The envelope's spatial reference, if// if has one, will be used as the TIN's spatial reference. If it is not set, as in this case,// the TIN's spatial reference will be unknown.
            IEnvelope Env = new EnvelopeClass();
            //获取点集的范围
            double minx = double.PositiveInfinity;
            double miny = double.PositiveInfinity;
            double maxx = double.NegativeInfinity;
            double maxy = double.NegativeInfinity;
            foreach (TriNode curPoint in map.TriNodeList)
            {
                if (curPoint.X < minx)
                    minx = curPoint.X;
                if (curPoint.Y < miny)
                    miny = curPoint.Y;
                if (curPoint.X > maxx)
                    maxx = curPoint.X;
                if (curPoint.Y > maxy)
                    maxy = curPoint.Y;

            }
            Env.PutCoords(minx, miny, maxx, maxy);

            TinEdit.InitNew(Env);
            IFeatureClass pointfcls, polylinefcls, polygonfcls;
            map.Export2FeatureClasses(out pointfcls, out polylinefcls, out polygonfcls);
            object o = Type.Missing;
            if (pointfcls != null)
            {
                TinEdit.AddFromFeatureClass(pointfcls, null, null, null, esriTinSurfaceType.esriTinMassPoint, ref o);
            }
            if (polylinefcls != null)
            {
                TinEdit.AddFromFeatureClass(polylinefcls, null, null, null, esriTinSurfaceType.esriTinSoftLine, ref o);
            }
            if (polygonfcls != null)
            {
                TinEdit.AddFromFeatureClass(polygonfcls, null, null, null, esriTinSurfaceType.esriTinSoftLine, ref o);
            }

            //#region AddfromFeatureClass
            //object o = Type.Missing;

            //foreach (IFeatureLayer curLyr in lyrList)
            //{
            //    IFeatureCursor cursor = null;
            //    switch (curLyr.FeatureClass.ShapeType)
            //    {

            //        case esriGeometryType.esriGeometryPoint:
            //            {
            //                #region 点要素

            //                IFields pfields = curLyr.FeatureClass.Fields;
            //                IField pHeightField = null;
            //                for (int i = 0; i < pfields.FieldCount; i++)
            //                {
            //                    IField curField = pfields.get_Field(i);
            //                    if (curField.Name == "OBJECTID")
            //                    {
            //                        pHeightField = curField;
            //                        break;
            //                    }
            //                }

            //                TinEdit.AddFromFeatureClass(curLyr.FeatureClass, null, pHeightField, pHeightField, esriTinSurfaceType.esriTinMassPoint, ref o);

            //                #endregion
            //                break;
            //            }
            //        case esriGeometryType.esriGeometryPolyline:
            //            {

            //                #region 线要素
            //                cursor = curLyr.Search(null, false);

            //                IFields pfields = curLyr.FeatureClass.Fields;
            //                IField pHeightField = null;
            //                for (int i = 0; i < pfields.FieldCount; i++)
            //                {
            //                    IField curField = pfields.get_Field(i);
            //                    if (curField.Name == "OBJECTID")
            //                    {
            //                        pHeightField = curField;
            //                        break;
            //                    }
            //                }
            //                TinEdit.AddFromFeatureClass(curLyr.FeatureClass, null, pHeightField, pHeightField, esriTinSurfaceType.esriTinSoftLine, ref o);

            //                #endregion
            //                break;
            //            }

            //        case esriGeometryType.esriGeometryPolygon:
            //            {

            //                #region 面要素
            //                IFields pfields = curLyr.FeatureClass.Fields;
            //                IField pHeightField = null;
            //                for (int i = 0; i < pfields.FieldCount; i++)
            //                {
            //                    IField curField = pfields.get_Field(i);
            //                    if (curField.Name == "OBJECTID")
            //                    {
            //                        pHeightField = curField;
            //                        break;
            //                    }
            //                }
            //                TinEdit.AddFromFeatureClass(curLyr.FeatureClass, null, pHeightField, pHeightField, esriTinSurfaceType.esriTinSoftLine, ref o);

            //                #endregion
            //                break;
            //            }
            //    }
            //}
            //#endregion

            o = true;
          //  TinEdit.StopEditing(true);
            object overwrite = true;
            TinEdit.SaveAs(TinPath, ref overwrite); //写入文件

            ITinAdvanced itina = (TinEdit as ITinAdvanced);

            int NodeCount = itina.NodeCount;
            for (int i = 1; i <= NodeCount; i++)
            {
                ITinNode curNode = itina.GetNode(i);
                int tag = curNode.TagValue;
            }

            int EdgeCount = itina.EdgeCount;
            for (int i = 1; i <= EdgeCount; i++)
            {
                ITinEdge curEdge = itina.GetEdge(i);
                int tag = curEdge.TagValue;
            }

            int TriCount = itina.TriangleCount;
            for (int i = 1; i <= TriCount; i++)
            {
                ITinTriangle curTriangle = itina.GetTriangle(i);

                ITinNode p1 = curTriangle.get_Node(1);
                ITinNode p2 = curTriangle.get_Node(2);
                ITinNode p3 = curTriangle.get_Node(3);

                TriNode point1 = TriNode.GetNode(map.TriNodeList, p1);
                TriNode point2 = TriNode.GetNode(map.TriNodeList, p2);
                TriNode point3 = TriNode.GetNode(map.TriNodeList, p3);

                if (point1 != null && point2 != null && point3 != null)
                {
                    Triangle tri = new Triangle();
                    tri.point1 = point1;
                    tri.point2 = point2;
                    tri.point3 = point3;

                    //第一条边
                    TriEdge e1 = new TriEdge(point1, point2);
                    if (point1.TagValue == point2.TagValue && point1.FeatureType == point2.FeatureType&&point1.TagValue!=-1)
                    {
                        e1.tagID = point1.TagValue;
                        e1.FeatureType = point1.FeatureType;
                    }
                    else
                    {
                        FeatureType ftype = FeatureType.Unknown;
                        int tagvalue=map.GetConsEdge(point1, point2,out ftype);
                        if (tagvalue != -1)
                        {
                            e1.tagID = tagvalue;
                            e1.FeatureType = ftype;
                        }
                    }
                    TriEdgeList.Add(e1);

                    //第二条边
                    TriEdge e2 = new TriEdge(point2, point3);
                    if (point2.TagValue == point3.TagValue && point2.FeatureType == point3.FeatureType && point2.TagValue != -1)
                    {
                        e2.tagID = point2.TagValue;
                        e2.FeatureType = point2.FeatureType;
                    }
                    else
                    {
                        FeatureType ftype = FeatureType.Unknown;
                        int tagvalue = map.GetConsEdge(point2, point3, out ftype);
                        if (tagvalue != -1)
                        {
                            e2.tagID = tagvalue;
                            e2.FeatureType = ftype;
                        }
                    }
                    TriEdgeList.Add(e2);

                    //第三条边
                    TriEdge e3 = new TriEdge(point3, point1);
                    if (point3.TagValue == point1.TagValue && point3.FeatureType == point1.FeatureType && point3.TagValue != -1)
                    {
                        e3.tagID = point3.TagValue;
                        e3.FeatureType = point3.FeatureType;
                    }
                    else
                    {
                        FeatureType ftype = FeatureType.Unknown;
                        int tagvalue = map.GetConsEdge(point3, point1, out ftype);
                        if (tagvalue != -1)
                        {
                            e3.tagID = tagvalue;
                            e3.FeatureType = ftype;
                        }
                    }
                    TriEdgeList.Add(e3);

                    tri.edge1 = e1;
                    tri.edge2 = e2;
                    tri.edge3 = e3;
                    e1.leftTriangle = tri;
                    e2.leftTriangle = tri;
                    e3.leftTriangle = tri;
                    this.TriangleList.Add(tri);
                }
                //设置对偶边和右边三角形
                foreach (TriEdge edge in TriEdgeList)
                {
                    if (edge.doulEdge != null)
                        continue;
                    TriEdge doulEdge = TriEdge.FindOppsiteEdge(TriEdgeList, edge);
                    if (doulEdge == null)
                    {
                        edge.doulEdge = null;
                        edge.rightTriangle = null;
                    }
                    else
                    {
                        edge.doulEdge = doulEdge;
                        edge.rightTriangle = doulEdge.leftTriangle;
                    }
                }
            }

        }

        #endregion

        #region 扩边法
        /// <summary>
        /// 扩边法创建三角网
        /// </summary>
        private void CreateDelaunayTin_Side_extent()
        {
            if (this.TriNodeCount < 3)
                return;
            FirstTriangle();//创建第一个三角形
            BuildDelaunay();//扩边创建三角网
            TriangleList = Triangle.EliDuplicateTris(TriangleList);
            TriEdgeList.Clear();
            TriEdgeList = Triangle.GetEdges(TriangleList);
            TriEdge.AmendEdgeLeftTriangle(TriEdgeList);
        }

        /// <summary>
        /// 顾及了点在同一条直线上
        /// </summary>
        private void CreateDelaunayTin_Side_extent2()
        {
            if (this.TriNodeCount < 3)
                return;
            FirstTriangle();//创建第一个三角形
            BuildDelaunay2();//扩边创建三角网
            TriEdgeList.Clear();
            TriEdgeList = Triangle.GetEdges(TriangleList);
            TriEdge.AmendEdgeLeftTriangle(TriEdgeList);//修改边的属性
        }

        /// <summary>
        /// 构建第一个三角形
        /// </summary>
        private void FirstTriangle()
        {
            int index = -1; 
            //TriNode pTriNode = null;
            double length = double.MaxValue;
            #region 找到与第一个点最近的点，构成一条边
            foreach (TriNode p1 in TriNodeList)
            {
                double temp = TriEdge.LengthSquare(TriNodeList[0], p1);
                if (temp != 0 && temp < length)
                {
                    index = p1.ID;
                    //pTriNode = p1;
                    length = temp;
                }
            }
            TriNode point1, point2, point3;
            point1 = TriNodeList[0];
            //point3 = TriNodeList[index];
            point3=TriNode.GetTriNodebyID(this.TriNodeList, index);
            //point3 = pTriNode;
            #endregion

            if (point1 != null && point3 != null)
            {
                TriEdge edge = new TriEdge(point1, point3);//创建一条边
                point2 = TriEdge.GetBestPoint2(edge, TriNodeList);

                //如果右边有点
                if (point2 != null)
                {
                    TriEdge triEdge1 = new TriEdge(point1, point2);
                    TriEdge triEdge2 = new TriEdge(point2, point3);
                    TriEdge triEdge3 = new TriEdge(point3, point1);
                    Triangle triangle = new Triangle(point1, point2, point3);
                    //将三条边的引用赋值给该三角形
                    triangle.edge1 = triEdge1;
                    triangle.edge2 = triEdge2;
                    triangle.edge3 = triEdge3;
                    //将该三角形作为每条边的左三角形（边的方向保持为逆时针方向）
                    triEdge1.leftTriangle = triangle;
                    triEdge2.leftTriangle = triangle;
                    triEdge3.leftTriangle = triangle;
                    //此时加入的边可能有重复
                    TriEdgeList.Add(triEdge1);
                    TriEdgeList.Add(triEdge2);
                    TriEdgeList.Add(triEdge3);
                    TriangleList.Add(triangle);
                }
                else
                {
                    //如果右边没有点
                    edge = new TriEdge(point3, point1);
                    point2 = TriEdge.GetBestPoint2(edge, TriNodeList);
                    TriEdge triEdge1 = new TriEdge(point3, point2);
                    TriEdge triEdge2 = new TriEdge(point2, point1);
                    TriEdge triEdge3 = new TriEdge(point1, point3);
                    Triangle triangle = new Triangle(point3, point2, point1);
                    triangle.edge1 = triEdge1;
                    triangle.edge2 = triEdge2;
                    triangle.edge3 = triEdge3;
                    triEdge1.leftTriangle = triangle;
                    triEdge2.leftTriangle = triangle;
                    triEdge3.leftTriangle = triangle;
                    //此时加入的边可能有重复
                    TriEdgeList.Add(triEdge1);
                    TriEdgeList.Add(triEdge2);
                    TriEdgeList.Add(triEdge3);
                    TriangleList.Add(triangle);
                }
            }   
        }
       
        private void BuildDelaunay()
        {
            int i = 0;
            while (TriEdgeList.Count != 0)
            {
                TriEdge edge = TriEdgeList[0];
                TriNode point2  = TriEdge.GetBestPoint(edge, TriNodeList);

                #region
                if (point2 != null)
                {
                    Triangle triangle = new Triangle(edge.startPoint, point2, edge.endPoint);

                    //避免数据精度导致重复加入已存在三角形而陷入死循环的情况
                    if (triangle.isContainedINTris(TriangleList))
                    {
                        TriEdgeList.Remove(edge);
                        continue;
                    }

                    TriEdge edge1 = new TriEdge(edge.startPoint, point2);
                    TriEdge edge2 = new TriEdge(point2, edge.endPoint);
                    TriEdge edge3 = new TriEdge(edge.endPoint, edge.startPoint);
                    edge1.leftTriangle = triangle;
                    edge2.leftTriangle = triangle;
                    edge3.leftTriangle = triangle;
                    triangle.edge1 = edge1;
                    triangle.edge2 = edge2;
                    triangle.edge3 = edge3;
                    edge3.rightTriangle = edge.leftTriangle;
                    edge.rightTriangle = edge3.leftTriangle;
                    TriEdgeList.Remove(edge);
                    if (!TriangleList.Contains(triangle))
                    {
                        TriangleList.Add(triangle);
                    }
                    TriEdge edgeTemp = new TriEdge();
                    edgeTemp.startPoint = edge1.endPoint;
                    edgeTemp.endPoint = edge1.startPoint;
                    TriEdge sameEdge = TriEdge.FindSameEdge(TriEdgeList, edgeTemp);
                    if (sameEdge == null)
                    {
                        TriEdgeList.Add(edge1);
                    }
                    else
                    {
                        TriEdgeList.Remove(sameEdge);
                    }
                    edgeTemp.startPoint = edge2.endPoint;
                    edgeTemp.endPoint = edge2.startPoint;
                    sameEdge = TriEdge.FindSameEdge(TriEdgeList, edgeTemp);
                    if (sameEdge == null)
                    {
                        TriEdgeList.Add(edge2);
                    }
                    else
                    {
                        TriEdgeList.Remove(sameEdge);
                    }

                    i++;
                }
                #endregion

                #region
                else
                {
                    TriEdgeList.Remove(edge);
                }
                #endregion
            }
        }

        /// <summary>
        /// 考虑点在一条直线上的情况
        /// </summary>
        private void BuildDelaunay2()
        {
            while (TriEdgeList.Count != 0)//边的条数不等于0
            {
                TriEdge edge = TriEdgeList[0];
                TriNode point2 = new TriNode();
                point2 = TriEdge.GetBestPoint2(edge, TriNodeList);//获得最佳的第三点

                #region 加入一个三角形
                if (point2 != null)
                {
                    Triangle triangle = new Triangle(edge.startPoint, point2, edge.endPoint);

                    //避免数据精度导致重复加入已存在三角形而陷入死循环的情况
                    if (triangle.isContainedINTris(TriangleList))
                    {
                        TriEdgeList.Remove(edge);
                        continue;
                    }

                    //附加一个判断条件（如果与已有的三角形相交面积大于0，则也不加入）
                    if (triangle.IsIntersectAsPolygon(TriangleList))
                    {
                        TriEdgeList.Remove(edge);
                        continue;
                    }

                    //附加一个判断条件（如果三角形是在同一条直线上的三角形，则不加入）
                    if (triangle.IsOnLine())
                    {
                        TriEdgeList.Remove(edge);
                        continue;
                    }

                    TriEdge edge1 = new TriEdge(edge.startPoint, point2);
                    TriEdge edge2 = new TriEdge(point2, edge.endPoint);
                    TriEdge edge3 = new TriEdge(edge.endPoint, edge.startPoint);
                    edge1.leftTriangle = triangle;
                    edge2.leftTriangle = triangle;
                    edge3.leftTriangle = triangle;
                    triangle.edge1 = edge1;
                    triangle.edge2 = edge2;
                    triangle.edge3 = edge3;
                    edge3.rightTriangle = edge.leftTriangle;
                    edge.rightTriangle = edge3.leftTriangle;
                    TriEdgeList.Remove(edge);
                    if (!TriangleList.Contains(triangle))
                    {
                        TriangleList.Add(triangle);
                    }

                    TriEdge edgeTemp = new TriEdge();
                    edgeTemp.startPoint = edge1.endPoint;
                    edgeTemp.endPoint = edge1.startPoint;
                    TriEdge sameEdge = TriEdge.FindSameEdge(TriEdgeList, edgeTemp);
                    if (sameEdge == null)
                    {
                        TriEdgeList.Add(edge1);
                    }
                    else
                    {
                        TriEdgeList.Remove(sameEdge);
                    }
                    edgeTemp.startPoint = edge2.endPoint;
                    edgeTemp.endPoint = edge2.startPoint;
                    sameEdge = TriEdge.FindSameEdge(TriEdgeList, edgeTemp);
                    if (sameEdge == null)
                    {
                        TriEdgeList.Add(edge2);
                    }
                    else
                    {
                        TriEdgeList.Remove(sameEdge);
                    }
                }
                #endregion

                #region 移除一条边
                else
                {
                    TriEdgeList.Remove(edge);
                }
                #endregion
            }
        }

        #endregion

        /// <summary>
        /// 写ID
        /// </summary>
        public void WriteID()
        {
            Triangle.WriteID(this.TriangleList);
            TriEdge.WriteID(this.TriEdgeList);
        }
        /// <summary>
        /// 将结果写入Shape文件
        /// </summary>
        public void WriteShp(ISpatialReference pSpatialReference)
        {
            TriNode.Create_WriteVetex2Shp(@"E:\DelaunayShape", @"Vextex", this.TriNodeList, pSpatialReference);
            TriEdge.Create_WriteEdge2Shp(@"E:\DelaunayShape", @"Edge", this.TriEdgeList, pSpatialReference);
            Triangle.Create_WriteTriange2Shp(@"E:\DelaunayShape", @"Triangle", this.TriangleList, pSpatialReference);
        }

        /// <summary>
        /// 将结果写入Shape文件
        /// </summary>
        public void WriteShp(string filepath, ISpatialReference pSpatialReference)
        {
            if (!Directory.Exists(filepath))
                Directory.CreateDirectory(filepath);
            TriNode.Create_WriteVetex2Shp(filepath, @"cVextex", this.TriNodeList, pSpatialReference);
            TriEdge.Create_WriteEdge2Shp(filepath, @"cEdge", this.TriEdgeList, pSpatialReference);
            Triangle.Create_WriteTriange2Shp(filepath, @"cTriangle", this.TriangleList, pSpatialReference);
        }

        /// <summary>
        /// 邻近图的三角形结构
        /// </summary>
        public class GraphTriangle
        {
            public int ID;
            public List<TriEdge> EdgeList = new List<TriEdge>();
        }

     

        /// <summary>
        /// 创建AlphaShape
        /// 边不能有重复（重复会出问题）
        /// </summary>
        /// <param name="Distance"></param>
        /// <returns></returns>
        public List<TriEdge> CreateAlphaShape(double Distance)
        {
            //this.DeleteRepeatedEdge(this.TriEdgeList);
            Dictionary<TriEdge, List<Triangle>> TriangleDic = new Dictionary<TriEdge, List<Triangle>>();//存储边及其邻接三角形
            List<TriEdge> LongerPeList = new List<TriEdge>();//存储较长的边界边
            List<TriEdge> AlphaShapeEdgeList = new List<TriEdge>();

            #region 为每个三角形添加EdgeList
            for (int i = 0; i < this.TriangleList.Count; i++)
            {
                TriangleList[i].EdgeList.Clear();
                TriangleList[i].EdgeList.Add(TriangleList[i].edge1);
                TriangleList[i].EdgeList.Add(TriangleList[i].edge2);
                TriangleList[i].EdgeList.Add(TriangleList[i].edge3);
            }
            #endregion

            #region 找到每条边对应的邻接三角形
            for (int i = 0; i < this.TriEdgeList.Count; i++)
            {
                List<Triangle> EdgeTriangle = new List<Triangle>();

                if (this.TriEdgeList[i].leftTriangle != null)
                {
                    EdgeTriangle.Add(this.TriEdgeList[i].leftTriangle);
                }
                if (this.TriEdgeList[i].rightTriangle != null)
                {
                    EdgeTriangle.Add(this.TriEdgeList[i].rightTriangle);
                }

                TriangleDic.Add(this.TriEdgeList[i], EdgeTriangle);
            }
            #endregion

            #region 找到长度大于给定长度的边界边
            for (int i = 0; i < this.TriEdgeList.Count; i++)
            {
                if (this.TriEdgeList[i].Length > Distance && (this.TriEdgeList[i].leftTriangle==null || this.TriEdgeList[i].rightTriangle==null))
                {
                    LongerPeList.Add(this.TriEdgeList[i]);
                }
            }
            #endregion

            #region 遍历删除边
            while (LongerPeList.Count > 0)
            {
                if (TriangleDic[LongerPeList[0]].Count == 1)
                {
                    Triangle pTriangle = TriangleDic[LongerPeList[0]][0];//找到给定边的邻接三角形T
                    pTriangle.EdgeList.Remove(LongerPeList[0]);//找到另外两条边，并将它们的邻接三角形删除T

                    TriangleDic[pTriangle.EdgeList[0]].Remove(pTriangle);
                    TriangleDic[pTriangle.EdgeList[1]].Remove(pTriangle);

                    //将两条边中大于给定距离的边加入队列
                    if (pTriangle.EdgeList[0].Length > Distance && TriangleDic[pTriangle.EdgeList[0]].Count == 1)
                    {
                        if (!LongerPeList.Contains(pTriangle.EdgeList[0]))
                        {
                            LongerPeList.Add(pTriangle.EdgeList[0]);
                        }
                    }

                    if (pTriangle.EdgeList[1].Length > Distance && TriangleDic[pTriangle.EdgeList[1]].Count == 1)
                    {
                        if (!LongerPeList.Contains(pTriangle.EdgeList[1]))
                        {
                            LongerPeList.Add(pTriangle.EdgeList[1]);
                        }
                    }
                }

                LongerPeList.RemoveAt(0);
            }
            #endregion

            #region 找到当前列表中长度小于阈值的边界边
            for (int i = 0; i < this.TriEdgeList.Count; i++)
            {
                if (this.TriEdgeList[i].Length< Distance && TriangleDic[this.TriEdgeList[i]].Count == 1)
                {
                    AlphaShapeEdgeList.Add(this.TriEdgeList[i]);
                }
            }
            #endregion

            return AlphaShapeEdgeList;
        }

        /// <summary>
        /// 创建AlphaShape
        /// 边不能有重复（重复会出问题）
        /// </summary>
        /// <param name="Distance"></param>
        /// <returns></returns>
        public List<TriEdge> CreateAlphaShape2(double Distance)
        {
            this.DeleteRepeatedEdge(this.TriEdgeList);
            List<GraphTriangle> TriangleList = this.GetTriangleForGraph(this.TriNodeList,this.TriEdgeList);

            Dictionary<TriEdge, List<GraphTriangle>> TriangleDic = new Dictionary<TriEdge, List<GraphTriangle>>();//存储边及其邻接三角形
            List<TriEdge> LongerPeList = new List<TriEdge>();//存储较长的边界边
            List<TriEdge> AlphaShapeEdgeList = new List<TriEdge>();

            #region 找到每条边对应的邻接三角形
            for (int i = 0; i < this.TriEdgeList.Count; i++)
            {
                List<GraphTriangle> EdgeTriangle = new List<GraphTriangle>();

                for (int j = 0; j < TriangleList.Count; j++)
                {
                    if (TriangleList[j].EdgeList.Contains(this.TriEdgeList[i]))
                    {
                        EdgeTriangle.Add(TriangleList[j]);
                    }
                }

                TriangleDic.Add(this.TriEdgeList[i], EdgeTriangle);
            }
            #endregion

            #region 找到长度大于给定长度的边界边
            for (int i = 0; i < this.TriEdgeList.Count; i++)
            {
                if (this.TriEdgeList[i].Length > Distance && TriangleDic[this.TriEdgeList[i]].Count == 1)
                {
                    LongerPeList.Add(this.TriEdgeList[i]);
                }
            }
            #endregion

            #region 遍历删除边
            while (LongerPeList.Count > 0)
            {
                if (TriangleDic[LongerPeList[0]].Count == 1)
                {
                    GraphTriangle pGraphTriangle = TriangleDic[LongerPeList[0]][0];//找到给定边的邻接三角形T
                    pGraphTriangle.EdgeList.Remove(LongerPeList[0]);//找到另外两条边，并将它们的邻接三角形删除T

                    TriangleDic[pGraphTriangle.EdgeList[0]].Remove(pGraphTriangle);
                    TriangleDic[pGraphTriangle.EdgeList[1]].Remove(pGraphTriangle);

                    //将两条边中大于给定距离的边加入队列
                    if (pGraphTriangle.EdgeList[0].Length > Distance && TriangleDic[pGraphTriangle.EdgeList[0]].Count == 1)
                    {
                        LongerPeList.Add(pGraphTriangle.EdgeList[0]);
                    }

                    if (pGraphTriangle.EdgeList[1].Length > Distance && TriangleDic[pGraphTriangle.EdgeList[1]].Count == 1)
                    {
                        LongerPeList.Add(pGraphTriangle.EdgeList[1]);
                    }
                }

                LongerPeList.RemoveAt(0);
            }
            #endregion

            #region 找到当前列表中长度小于阈值的边界边
            for (int i = 0; i < this.TriEdgeList.Count; i++)
            {
                if (this.TriEdgeList[i].Length < Distance && TriangleDic[this.TriEdgeList[i]].Count == 1)
                {
                    AlphaShapeEdgeList.Add(this.TriEdgeList[i]);
                }
            }
            #endregion

            return AlphaShapeEdgeList;
        }

        /// <summary>
        /// 将AlphaShape转成Polygon
        /// </summary>
        /// <param name="EdgeList"></param>
        /// <returns></returns>
        public List<PolygonObject> GetAlphaShape(List<TriEdge> EdgeList)
        {
            int Label = 0;
            List<PolygonObject> PoList=new List<PolygonObject>();
            while (EdgeList.Count > 0)
            {
                bool Extend = true;

                List<TriNode> NodeList = new List<TriNode>();
                TriEdge FirstEdge = EdgeList[0];
                TriNode StartPoint = FirstEdge.startPoint;
                TriNode EndPoint = FirstEdge.endPoint;
                NodeList.Add(StartPoint); NodeList.Add(EndPoint);
                while (Extend)
                {                  
                    for (int i = 1; i < EdgeList.Count; i++)
                    {
                        if (EdgeList[i].startPoint.TagValue == EndPoint.TagValue)
                        {
                            NodeList.Add(EdgeList[i].startPoint);
                            EndPoint = EdgeList[i].endPoint;
                            if (EdgeList[i].endPoint.TagValue == StartPoint.TagValue)
                            {
                                NodeList.Add(StartPoint);
                                Extend = false;
                            }
                            EdgeList.RemoveAt(i);
                            break;
                        }

                        if (EdgeList[i].endPoint.TagValue == EndPoint.TagValue)
                        {
                            NodeList.Add(EdgeList[i].endPoint);
                            EndPoint = EdgeList[i].startPoint;
                            if (EdgeList[i].startPoint.TagValue == StartPoint.TagValue)
                            {
                                NodeList.Add(StartPoint);
                                Extend = false;
                            }
                            EdgeList.RemoveAt(i);
                            break;
                        }
                    }
                }

                PolygonObject Po = new PolygonObject(Label,NodeList);
                PoList.Add(Po);
                Label++;
                EdgeList.RemoveAt(0);
            }

            return PoList;
        }

        /// <summary>
        /// 求给定邻近图的三角形列表
        /// </summary>
        /// <param name="PnList"></param>
        /// <param name="PeList"></param>
        /// <returns></returns>
        public List<GraphTriangle> GetTriangleForGraph(List<TriNode> PnList, List<TriEdge> PeList)
        {
            List<TriEdge> PfList = new List<TriEdge>();
            for (int i = 0; i < PeList.Count; i++)
            {
                PfList.Add(PeList[i]);
            }

            List<GraphTriangle> GraphTriangleList = new List<GraphTriangle>();

            while (PfList.Count > 0)
            {
                int TriangleId = 0;

                TriNode Pn1 = PfList[0].startPoint;
                TriNode Pn2 = PfList[0].endPoint;
                for (int j = 0; j < PnList.Count; j++)
                {
                    #region 判断是否是三角形的第三点
                    bool Label1 = false; bool Label2 = false;
                    TriEdge ProxiEdge1 = null; TriEdge ProxiEdge2 = null;
                    TriNode mPn = PnList[j];
                    if (mPn.X != Pn1.X && mPn.X != Pn2.X)
                    {
                        for (int m = 0; m < PfList.Count; m++)
                        {
                            #region mPn与Pn1是否为边
                            if ((mPn.TagValue == PfList[m].startPoint.TagValue && Pn1.TagValue == PfList[m].endPoint.TagValue) || mPn.TagValue == PfList[m].endPoint.TagValue && Pn1.TagValue == PfList[m].startPoint.TagValue)
                            {
                                Label1 = true;
                                ProxiEdge1 = PfList[m];
                            }
                            #endregion

                            #region mPn与Pn2是否为边
                            if ((mPn.TagValue == PfList[m].startPoint.TagValue && Pn2.TagValue == PfList[m].endPoint.TagValue) || mPn.TagValue == PfList[m].endPoint.TagValue && Pn2.TagValue == PfList[m].startPoint.TagValue)
                            {
                                Label2 = true;
                                ProxiEdge2 = PfList[m];
                            }
                            #endregion
                        }
                    }
                    #endregion

                    #region 若是第三点，添加该三角形
                    if (Label1 && Label2)
                    {
                        GraphTriangle pGraphTriangle = new GraphTriangle();
                        pGraphTriangle.ID = TriangleId;
                        TriangleId = TriangleId + 1;
                        pGraphTriangle.EdgeList.Add(PfList[0]);
                        pGraphTriangle.EdgeList.Add(ProxiEdge1);
                        pGraphTriangle.EdgeList.Add(ProxiEdge2);

                        GraphTriangleList.Add(pGraphTriangle);
                    }
                    #endregion
                }

                PfList.RemoveAt(0);
            }

            return GraphTriangleList;
        }

        /// <summary>
        /// 删除邻近图中的重复边（最短距离较大的边被删除）另外，目前最多只处理三条重复边的情况
        /// </summary>
        public void DeleteRepeatedEdge(List<TriEdge> PeList)
        {
            #region 将PeList添加入Dictionary中
            Dictionary<TriEdge, bool> EdgeDic = new Dictionary<TriEdge, bool>();

            for (int i = 0; i < PeList.Count; i++)
            {
                EdgeDic.Add(PeList[i], false);
            }
            #endregion

            #region 将重复的边标记为删除
            for (int i = 0; i < PeList.Count; i++)
            {
                TriEdge Pe1 = PeList[i];
                if (!EdgeDic[Pe1])
                {
                    for (int j = 0; j < PeList.Count; j++)
                    {
                        if (j != i)
                        {
                            TriEdge Pe2 = PeList[j];

                            if (!EdgeDic[Pe2])
                            {
                                if ((Pe1.startPoint.ID == Pe2.startPoint.ID && Pe1.endPoint.ID == Pe2.endPoint.ID) ||
                                    (Pe1.startPoint.ID == Pe2.endPoint.ID && Pe1.endPoint.ID == Pe2.startPoint.ID))
                                {
                                    EdgeDic[Pe2] = true;
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region 将删除的边在PeList中删除
            foreach (KeyValuePair<TriEdge, bool> kvp in EdgeDic)
            {
                if (kvp.Value)
                {
                    PeList.Remove(kvp.Key);
                }
            }
            #endregion
        }

    }
}
