using Cinteros.Xrm.CRMWinForm;
using Cinteros.XTB.PluginTraceViewer.Const;
using McTools.Xrm.Connection;
using McTools.Xrm.Connection.WinForms;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
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
            button4.Enabled = crmGridView1.OrganizationService != null;
            button5.Enabled = crmGridView1.OrganizationService != null;
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

        private void button3_Click(object sender, EventArgs e)
        {
            crmGridView1.AutoRefresh = false;
            crmGridView1.FilterColumns = textBox1.Text;
            crmGridView1.FilterText = textBox2.Text;
            textBox1.Text = crmGridView1.FilterColumns;
            textBox2.Text = crmGridView1.FilterText;
            crmGridView1.AutoRefresh = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var fetch = @"<fetch top='50' aggregate='true' >
  <entity name='opportunity' >
    <attribute name='estimatedvalue' alias='Total' aggregate='sum' />
    <order alias='Total' />
    <link-entity name='account' from='accountid' to='customerid' >
      <attribute name='name' alias='acname' groupby='true' />
    </link-entity>
  </entity>
</fetch>";
            var ec = crmGridView1.OrganizationService.RetrieveMultiple(new FetchExpression(fetch));
            crmGridView1.DataSource = ec;
            crmGridView2.DataSource = ec;
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            var QEplugintracelog = new QueryExpression(PluginTraceLog.EntityName);
            QEplugintracelog.TopCount = 50;
            QEplugintracelog.ColumnSet.AddColumns(
                            //PluginTraceLog.CorrelationId,
                            //PluginTraceLog.PerformanceExecutionStarttime,
                            //PluginTraceLog.PerformanceConstructorStarttime,
                            //PluginTraceLog.OperationType,
                            //PluginTraceLog.MessageName,
                            //PluginTraceLog.PrimaryKey,
                            //PluginTraceLog.PrimaryEntity,
                            //PluginTraceLog.ExceptionDetails,
                            //PluginTraceLog.MessageBlock,
                            //PluginTraceLog.PerformanceExecutionDuration,
                            //PluginTraceLog.CreatedOn,
                            //PluginTraceLog.Depth,
                            //PluginTraceLog.Mode,
                            //PluginTraceLog.RequestId,
                            PluginTraceLog.PrimaryName);
            var LEstep = QEplugintracelog.AddLink(SdkMessageProcessingStep.EntityName, PluginTraceLog.PluginStepId, SdkMessageProcessingStep.PrimaryKey, JoinOperator.LeftOuter);
            LEstep.EntityAlias = "step";
            LEstep.Columns.AddColumns(SdkMessageProcessingStep.PrimaryName, SdkMessageProcessingStep.Rank, SdkMessageProcessingStep.Stage);
            //filterControl.GetQueryFilter(QEplugintracelog, refreshOnly);
            QEplugintracelog.AddOrder(PluginTraceLog.PerformanceExecutionStarttime, OrderType.Descending);
            QEplugintracelog.AddOrder(PluginTraceLog.CorrelationId, OrderType.Ascending);    // This just to group threads together when starting the same second
            QEplugintracelog.AddOrder(PluginTraceLog.Depth, OrderType.Descending);           // This to try to compensate for executionstarttime only accurate to the second
            var ec = crmGridView1.OrganizationService.RetrieveMultiple(QEplugintracelog);
            crmGridView1.DataSource = ec;
        }
    }
}
