
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

using System.IO;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows;

using System.Reflection;
using System.Collections.ObjectModel;
using NPOI.SS.Util;
using Hyperwsn.Protocol;

namespace Hyperwsn.Protocol
{
    public class ExportXLS
    {

        private int CreateDataSheet(DataGrid SrcDataGrid, ObservableCollection<M1> data, ICellStyle cellStyle, ref HSSFWorkbook hssfworkbook, int iX, int Start, int Count)
        {
            IRow row;
            ICell cell;

            ISheet sheet = hssfworkbook.CreateSheet("数据" + (iX + 1).ToString());

            // 生成第一行
            row = sheet.CreateRow(0);
            List<String> bingdingPath = new List<string>();
            for (int c = 0; c < SrcDataGrid.Columns.Count; c++)
            {
                row.CreateCell(c).SetCellValue(SrcDataGrid.Columns[c].Header.ToString());
                row.Cells[c].CellStyle = cellStyle;

                double GridColumnActualWidth = SrcDataGrid.Columns[c].ActualWidth;
                double ExcelColumnActualWidth = GridColumnActualWidth * 40.0f;

                Int32 ExcelColumnActualWidth_i = Convert.ToInt32(ExcelColumnActualWidth);

                if (ExcelColumnActualWidth_i > 30000)
                {
                    sheet.SetColumnWidth(c, 30000);
                }
                else
                {
                    sheet.SetColumnWidth(c, ExcelColumnActualWidth_i);
                }
            }

            // 填充数据
            for (int r = 0; r < Count; r++)
            {
                if (r > 65535)
                {
                    return -1;
                }

                row = sheet.CreateRow(r + 1);       // CreateRow()的输入值最大是65535

                //用对象循环，而不是用DATAGrid中的数据循环
                for (int c = 0; c < SrcDataGrid.Columns.Count; c++)
                {
                    cell = row.CreateCell(c);
                    DataGridTextColumn dgcol = SrcDataGrid.Columns[c] as DataGridTextColumn;
                    Binding binding = (Binding)dgcol.Binding;
                    string path = binding.Path.Path;  //对象属性的名称拿到了
                    PropertyInfo info1 = data[Start + r].GetType().GetProperty(path);
                    if (info1 == null)
                    {
                        continue;
                    }

                    switch (info1.PropertyType.Name)
                    {
                        case "Byte":
                            byte byteValue = (byte)info1.GetValue(data[r], null);
                            row.CreateCell(c).SetCellValue(byteValue);
                            break;
                        case "UInt16":
                            UInt16 UInt16Value = (UInt16)info1.GetValue(data[r], null);
                            row.CreateCell(c).SetCellValue(UInt16Value);
                            break;
                        case "Int16":
                            Int16 Int16Value = (Int16)info1.GetValue(data[r], null);
                            row.CreateCell(c).SetCellValue(Int16Value);
                            break;
                        case "UInt32":
                            UInt32 UInt32Value = (UInt32)info1.GetValue(data[r], null);
                            row.CreateCell(c).SetCellValue(UInt32Value);
                            break;
                        case "Int32":
                            Int32 intValue = (Int32)info1.GetValue(data[r], null);
                            row.CreateCell(c).SetCellValue(intValue);
                            break;
                        case "Double":
                            double doubleValue = (double)info1.GetValue(data[r], null);
                            row.CreateCell(c).SetCellValue(doubleValue);
                            break;
                        default:
                            string cellString = info1.GetValue(data[r], null).ToString();
                            row.CreateCell(c).SetCellValue(cellString);
                            break;
                    }
                    row.Cells[c].CellStyle = cellStyle;
                }
            }

            return 0;
        }
        
        /// <summary>
        /// 带有备注
        /// </summary>
        /// <param name="SrcDataGrid"></param>
        /// <param name="FileName"></param>
        /// <param name="data"></param>
        /// <param name="comment"></param>
        /// <returns></returns>
        public int ExportWPFDataGrid(DataGrid SrcDataGrid, String FileName, ObservableCollection<M1> data, string comment)
        {
            if (SrcDataGrid == null || SrcDataGrid.Items.Count == 0)
            {
                return -1;
            }

            HSSFWorkbook hssfworkbook = new HSSFWorkbook();
            ICellStyle cellStyle = hssfworkbook.CreateCellStyle();
            cellStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cellStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cellStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cellStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;

            // 备注
            ISheet sheetOfComment = hssfworkbook.CreateSheet("备注");

            IRow row;
            ICell cell;
            int r = 0;
            int c = 0;

            string[] RowText = comment.Split('\n');

            for (r = 0; r < RowText.Length; r++)
            {
                c = 0;

                row = sheetOfComment.CreateRow(r);
                cell = row.CreateCell(c);

                row.CreateCell(c).SetCellValue(RowText[r]);
                row.Cells[c].CellStyle = cellStyle;
            }

            // 生成多个Sheet表
            const UInt16 ExportUnit = 50000;            // 一个Sheet表里最多可以存放的数据的数量
            int ExportTotal = data.Count;
            int ExportCount = 0;
            int ThisCount = 0;
            int iSheet = 0;

            while (ExportCount < ExportTotal)
            {
                if(ExportTotal - ExportCount < ExportUnit)
                {
                    ThisCount = ExportTotal - ExportCount;
                }
                else
                {
                    ThisCount = ExportUnit;
                }

                CreateDataSheet(SrcDataGrid, data, cellStyle, ref hssfworkbook, iSheet++, ExportCount, ThisCount);

                ExportCount += ThisCount;
            }

            FileStream file = null;
            try
            {
                file = new FileStream(FileName, FileMode.Create);
                hssfworkbook.Write(file);
            }
            catch (Exception)
            {
                
            }
            finally
            {
                if (file != null)
                {
                    file.Close();
                }
            }

            return 0;
        }

    //

    }
}


