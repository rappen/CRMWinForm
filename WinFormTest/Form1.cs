using Cinteros.Xrm.CRMWinForm;
using McTools.Xrm.Connection;
using McTools.Xrm.Connection.WinForms;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            ConnectionManager.Instance.ConnectionSucceed += Instance_ConnectionSucceed;
        }

        private void Instance_ConnectionSucceed(object sender, ConnectionSucceedEventArgs e)
        {
            crmGridView1.OrganizationService = e.OrganizationService;
            button2.Enabled = crmGridView1.OrganizationService != null;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var cs = new ConnectionSelector();
            if (cs.ShowDialog(this) == DialogResult.OK)
            {
                var conn = cs.SelectedConnections;
                ConnectionManager.Instance.ConnectToServer(conn, null);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var qex = new QueryExpression("account");
            qex.ColumnSet.AddColumns("name", "accountnumber", "primarycontactid", "accountratingcode", "numberofemployees", "revenue", "creditlimit");
            var pc = qex.AddLink("contact", "primarycontactid", "contactid", JoinOperator.LeftOuter);
            pc.Columns.AddColumn("emailaddress1");
            pc.EntityAlias = "C";
            var ec = crmGridView1.OrganizationService.RetrieveMultiple(qex);
            CalcSomeValue(ec);
            crmGridView1.DataSource = ec;
            crmGridView2.DataSource = ec;
        }

        private static void CalcSomeValue(EntityCollection ec)
        {
            var b = false;
            foreach (var ent in ec.Entities)
            {
                var calc = ent.Contains("revenue") ? ((Money)ent["revenue"]).Value : 0;
                if (b)
                {
                    ent["calc"] = (int)(calc * DateTime.Now.Minute);
                }
                else
                {
                    ent["calc"] = (double)(calc / DateTime.Now.Minute);
                }
                b = !b;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            crmGridView1.ShowFriendlyNames = checkBox1.Checked;
            crmGridView2.ShowFriendlyNames = checkBox1.Checked;
        }

        private void crmGridView1_RecordClick(object sender, CRMRecordEventArgs e)
        {
            var entity = e.Entity;
            var value = e.Value;
            if (entity != null)
            {
                MessageBox.Show("Entity: " + entity.Id + "\nValue: " + value);
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            crmGridView1.EntityReferenceClickable = checkBox2.Checked;
        }
    }
}
