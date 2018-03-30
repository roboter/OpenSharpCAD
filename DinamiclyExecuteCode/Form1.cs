using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace DynCode
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnRunCode_Click(object sender, EventArgs e)
        {
            Execute(txtCodeToRun.Text);
        }

        private void Execute(string code)
        {
            StringBuilder sb = new StringBuilder();

            //-----------------
            // Create the class as usual
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Windows.Forms;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("namespace CountryList");
            sb.AppendLine("{");

            sb.AppendLine("      public class SelectCountries");
            sb.AppendLine("      {");

            // My pre-defined class named FilterCountries that receive the sourceListBox
            sb.AppendLine("            public List<string> FilterCountries(ListBox countryList)");
            sb.AppendLine("            {");
            sb.AppendLine(code);
            sb.AppendLine("            }");
            sb.AppendLine("      }");
            sb.AppendLine("}");

            //-----------------
            // The finished code
            string classCode = sb.ToString();

            //-----------------
            // Dont need any extra assemblies

            dynamic classRef;
            try
            {
                txtErrors.Clear();

                //------------
                // Pass the class code, the namespace of the class and the list of extra assemblies needed
                classRef = CodeHelper.HelperFunction(classCode, "CountryList.SelectCountries", new object[] { });

                //-------------------
                // If the compilation process returned an error, then show to the user all errors
                if (classRef is CompilerErrorCollection)
                {
                    StringBuilder sberror = new StringBuilder();

                    foreach (CompilerError error in (CompilerErrorCollection)classRef)
                    {
                        sberror.AppendLine(string.Format("{0}:{1} {2} {3}", error.Line, error.Column, error.ErrorNumber, error.ErrorText));
                    }

                    txtErrors.Text = sberror.ToString();

                    return;
                }
            }
            catch (Exception ex)
            {
                // If something very bad happened then throw it
                MessageBox.Show(ex.Message);
                throw;
            }

            //-------------
            // Finally call the class to filter the countries with the specific routine provided
            List<string> targetValues = classRef.FilterCountries(lstSource);

            //-------------
            // Move the result to the target listbox
            lstTarget.Items.Clear();
            lstTarget.Items.AddRange(targetValues.ToArray());
        }
    }
}