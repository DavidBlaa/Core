﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using BExIS.Dlm.Entities.Data;
using BExIS.Dlm.Entities.DataStructure;
using BExIS.Dlm.Services.Data;
using BExIS.IO.Transform.Validation.DSValidation;
using BExIS.IO.Transform.Validation.Exceptions;
using BExIS.Xml.Helpers;
using DocumentFormat.OpenXml.Drawing.Charts;
using Vaiona.Utils.Cfg;

/// <summary>
///
/// </summary>        
namespace BExIS.IO.Transform.Output
{
    /// <summary>
    ///
    /// </summary>
    /// <remarks></remarks>        
    public class AsciiWriter:DataWriter
    {
        /// <summary>
        ///
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>        
        public TextSeperator Delimeter { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param>NA</param>       
        public AsciiWriter()
        {
            Delimeter = TextSeperator.comma;
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="delimeter"></param>
        public AsciiWriter(TextSeperator delimeter)
        {
            Delimeter = delimeter;
        }

        #region generic

        public static string CreateFile(string filepath)
        {
            string dataPath = Path.Combine(AppConfiguration.DataPath,filepath);

            try
            {
                if (!File.Exists(dataPath))
                {
                    File.Create(dataPath).Close();
                }

            }
            catch (Exception ex)
            {
                string message = ex.Message.ToString();
            }

            return dataPath;
        }

        public static bool AllTextToFile(string filepath, string text)
        {
            try
            {
                File.WriteAllText(filepath, text);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public string GenerateCsv(DataTable datatable, string fullpath)
        {
            string file = Path.GetFileName(fullpath);
            string ext = Path.GetExtension(fullpath);

            #region create

            try
            {
                if (!File.Exists(fullpath))
                {
                    File.Create(fullpath).Close();
                }

            }
            catch (Exception ex)
            {
                string message = ex.Message.ToString();
            }

            #endregion

            return fullpath;
        }

        #endregion

        #region bexis internal usage
        /// <summary>
        /// return the filepath
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="datasetId"></param>
        /// <param name="datasetVersionOrderNr"></param>
        /// <param name="dataStructureId"></param>
        /// <param name="title"></param>
        /// <param name="extention"></param>
        public string CreateFile(long datasetId, long datasetVersionOrderNr, long dataStructureId, string title, string extention)
        {
            string dataPath = GetFullStorePath(datasetId, datasetVersionOrderNr, title, extention);

            try
            {
                if (!File.Exists(dataPath))
                {
                    File.Create(dataPath).Close();
                }

            }
            catch (Exception ex)
            {
                string message = ex.Message.ToString();
            }

            return dataPath;
        }

        /// <summary>
        /// Add Datatuples and Datastructure to a Ascii file
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="dataTuples"> Datatuples to add</param>
        /// <param name="filePath">Path of the excel template file</param>
        /// <param name="dataStructureId">Id of datastructure</param>
        /// <returns>List of Errors or null</returns>
        public List<Error> AddDataTuples(DatasetManager datasetManager,List<long> dataTuplesIds, string filePath, long dataStructureId)
        {
            if (File.Exists(filePath))
            {
                StringBuilder data = new StringBuilder();
                data.AppendLine(dataStructureToRow(dataStructureId));

                DataTupleIterator tupleIterator = new DataTupleIterator(dataTuplesIds, datasetManager);
                foreach (var tuple in tupleIterator)
                {
                    string newline = datatupleToRow(tuple);
                    if (!String.IsNullOrEmpty(newline)) data.AppendLine(newline);
                }
                File.WriteAllText(filePath, data.ToString());
            }

            return ErrorMessages;
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="dataTuples"></param>
        /// <param name="filePath"></param>
        /// <param name="dataStructureId"></param>
        /// <returns></returns>
        public List<Error> AddDataTuples(List<AbstractTuple> dataTuples, string filePath, long dataStructureId)
        {
            if (File.Exists(filePath))
            {
                StringBuilder data = new StringBuilder();

                data.AppendLine(dataStructureToRow(dataStructureId));

                foreach (AbstractTuple dataTuple in dataTuples)
                {
                    data.AppendLine(datatupleToRow(dataTuple));
                }


                File.WriteAllText(filePath, data.ToString());
            }

            return ErrorMessages;
        }

        /// <summary>
        /// Convert Datatuple to  String line
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="id">Id of the Datatuple</param>
        /// <returns></returns>
        private string datatupleToRow(long id)
        {
            //DatatupleManager
            DatasetManager datasetManager = new DatasetManager();
            
            // I do not know where this function is called, but there is a chance that the id is referring to a tuple in a previous version, in that case, the tuple is not in the data tuples anymore. Javad
            DataTuple dataTuple = datasetManager.DataTupleRepo.Query(d=>d.Id.Equals(id)).FirstOrDefault();
            dataTuple.Materialize();

            #region ToDo David check the code inside

            //StringBuilder builder = new StringBuilder();
            //bool first = true;
            //string value = "";

            //foreach (VariableIdentifier vi in this.VariableIdentifiers)
            //{
            //    VariableValue vv = dataTuple.VariableValues.Where(v => v.Variable.Id.Equals(vi.id)).FirstOrDefault();
            //    if (vv.Value != null)
            //        value = vv.Value.ToString();
            //    // Add separator if this isn't the first value
            //    if (!first)
            //        builder.Append(AsciiHelper.GetSeperator(Delimeter));
            //    // Implement special handling for values that contain comma or quote
            //    // Enclose in quotes and double up any double quotes
            //    if (value.IndexOfAny(new char[] { '"', ',' }) != -1)
            //        builder.AppendFormat("\"{0}\"", value.Replace("\"", "\"\""));
            //    else
            //        builder.Append(value);
            //    first = false;
            //}


            //foreach (VariableValue vv in dataTuple.VariableValues)
            //{
            //    string value ="";
            //    if(vv.Value!=null)
            //        value =  vv.Value.ToString();
            //    // Add separator if this isn't the first value
            //    if (!first)
            //        builder.Append(AsciiHelper.GetSeperator(Delimeter));
            //    // Implement special handling for values that contain comma or quote
            //    // Enclose in quotes and double up any double quotes
            //    if (value.IndexOfAny(new char[] { '"', ',' }) != -1)
            //        builder.AppendFormat("\"{0}\"", value.Replace("\"", "\"\""));
            //    else
            //        builder.Append(value);
            //    first = false;
            //}

            #endregion

            return datatupleToRow(dataTuple);
        }

        /// <summary>
        /// Convert Datatuple to  String line
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="dataTuple"></param>
        /// <returns></returns>
        private string datatupleToRow(AbstractTuple dataTuple)
        {
            StringBuilder builder = new StringBuilder();
            StructuredDataStructure sds = GetDataStructure();

            bool first = true;
            string value = "";

            foreach (VariableIdentifier vi in this.VariableIdentifiers)
            {
                Variable variable = sds.Variables.Where(p => p.Id == vi.id).SingleOrDefault();

                if (variable != null)
                {
                    Dlm.Entities.DataStructure.DataType dataType = variable.DataAttribute.DataType;

                    VariableValue vv = dataTuple.VariableValues.Where(v => v.Variable.Id.Equals(vi.id)).FirstOrDefault();

                    if (vv !=null && vv.Value != null)
                    {
                        string format = GetStringFormat(dataType);
                        if (!string.IsNullOrEmpty(format))
                        {
                            value = GetFormatedValue(vv.Value, dataType, format);
                        }
                        else value = vv.Value.ToString();
                    }
                }
                // Add separator if this isn't the first value
                if (!first)
                    builder.Append(AsciiHelper.GetSeperator(Delimeter));
                // Implement special handling for values that contain comma or quote
                // Enclose in quotes and double up any double quotes
                if (value.IndexOfAny(new char[] {'"', ','}) != -1)
                    builder.AppendFormat("\"{0}\"", value.Replace("\"", "\"\""));
                else
                    builder.Append(value);
                first = false;
            }
  
            return builder.ToString();
        }
        
        /// <summary>
        /// Convert Datastructure to a String line
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="id"></param>
        /// <returns></returns>
        private string dataStructureToRow(long id)
        {
            StructuredDataStructure ds = GetDataStructure(id);
            StringBuilder builder = new StringBuilder();
            bool first = true;

            if (ds.Variables != null && ds.Variables.Any())
            {
                List<Variable> variables = ds.Variables.ToList();

                if (VisibleColumns != null)
                {
                    variables = GetSubsetOfVariables(ds.Variables.ToList(), VisibleColumns);
                }

                variables = SortVariablesOnDatastructure(variables, ds);

                foreach (Variable v in variables)
                {
                    string value = v.Label.ToString();
                    // Add separator if this isn't the first value
                    if (!first)
                        builder.Append(AsciiHelper.GetSeperator(Delimeter));
                    // Implement special handling for values that contain comma or quote
                    // Enclose in quotes and double up any double quotes
                    if (value.IndexOfAny(new char[] { '"', ',' }) != -1)
                        builder.AppendFormat("\"{0}\"", value.Replace("\"", "\"\""));
                    else
                        builder.Append(value);
                    first = false;

                    // add to variable identifiers
                    this.VariableIdentifiers.Add
                        (
                            new VariableIdentifier
                            {
                                id = v.Id,
                                name = v.Label,
                                systemType = v.DataAttribute.DataType.SystemType
                            }
                        );
                }
            }

            return builder.ToString();
        }

        private List<Variable> SortVariablesOnDatastructure(List<Variable> variables, DataStructure datastructure)
        {
            List<Variable> sortedVariables = new List<Variable>();

            XmlDocument extra = new XmlDocument();
            extra.LoadXml(datastructure.Extra.OuterXml);
            IEnumerable<XElement> elements = XmlUtility.GetXElementByNodeName("variable", XmlUtility.ToXDocument(extra));

            foreach (XElement element in elements)
            {
                long id = Convert.ToInt64(element.Value);
                Variable var =variables.Where(v => v.Id.Equals(id)).FirstOrDefault();
                if(var !=null)
                    sortedVariables.Add(var);
            }


            return sortedVariables;
        }

        #endregion

    }


}
