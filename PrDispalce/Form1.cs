using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Geodatabase;

namespace PrDispalce
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        #region 参数(图层参数)
        ILayer pLayer;
        #endregion

        #region 移除选中图层
        private void 移除图层ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (axMapControl1.Map.LayerCount > 0)
                {
                    if (pLayer != null)
                    {
                        axMapControl1.Map.DeleteLayer(pLayer);
                    }
                }
            }

            catch
            {
                MessageBox.Show("移除失败");
                return;
            }
        }
        #endregion

        #region 点击移除图层
        private void axTOCControl1_OnMouseDown_1(object sender, ITOCControlEvents_OnMouseDownEvent e)
        {
            if (axMapControl1.LayerCount > 0)
            {
                esriTOCControlItem pItem = new esriTOCControlItem();
                //pLayer = new FeatureLayerClass();
                IBasicMap pBasicMap = new MapClass();
                object pOther = new object();
                object pIndex = new object();
                // Returns the item in the TOCControl at the specified coordinates.
                axTOCControl1.HitTest(e.x, e.y, ref pItem, ref pBasicMap, ref pLayer, ref pOther, ref pIndex);
            }

            if (e.button == 2)
            {
                this.contextMenuStrip1.Show(axTOCControl1, e.x, e.y);
            }
        }
        #endregion


        public static T DeepCopy<T>(T obj)
        {
            object retval;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                //序列化成流
                bf.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                //反序列化成对象
                retval = bf.Deserialize(ms);
                ms.Close();
            }
            return (T)retval;
        }

        /// <summary>
        /// FlowMap
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void flowMapInitialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.FlowMap.FlowMap FM = new FlowMap.FlowMap(this.axMapControl1);
            FM.Show();
        }

        /// <summary>
        /// FlowFrm
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void flowMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.FlowMap.FlowMap FM = new FlowMap.FlowMap(this.axMapControl1);
            FM.Show();
        }

        /// <summary>
        /// CGFlowFrm
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cGFlowMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.CGFlowMap.CGFlowMap CGFM = new CGFlowMap.CGFlowMap(this.axMapControl1);
            CGFM.Show();
        }
    }
}
