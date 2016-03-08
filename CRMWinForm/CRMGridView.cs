using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System.Drawing;

namespace Cinteros.Xrm.CRMWinForm
{
    public partial class CRMGridView : DataGridView
    {
        private IOrganizationService organizationService;
        private EntityCollection entityCollection;
        private bool autoRefresh = true;
        private bool showFriendlyNames = false;
        private bool showIdColumn = true;
        private bool showIndexColumn = true;
        private bool entityReferenceClickable = false;
        private bool designedColumnsDetermined = false;
        private bool designedColumns = false;

        public CRMGridView()
        {
            InitializeComponent();
            ReadOnly = true;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToOrderColumns = true;
            AllowUserToResizeRows = false;
            CellClick += HandleRecordClick;
            CellDoubleClick += HandleRecordDoubleClick;
            CellMouseEnter += HandleCellMouseEnter;
            CellMouseLeave += HandleCellMouseLeave;
        }

        [Category("Data")]
        [DefaultValue(null)]
        public IOrganizationService OrganizationService
        {
            get { return organizationService; }
            set
            {
                organizationService = value;
                if (autoRefresh)
                {
                    Refresh();
                }
            }
        }

        [Category("Data")]
        [Description("Indicates the source of data (EntityCollection) for the CRMGridView control.")]
        public new object DataSource
        {
            get
            {
                if (entityCollection != null)
                {
                    return entityCollection;
                }
                return base.DataSource;
            }
            set
            {
                base.DataSource = value;
                entityCollection = value as EntityCollection;
                if (entityCollection != null && autoRefresh)
                {
                    Refresh();
                }
            }
        }

        [Category("Data")]
        [DefaultValue(true)]
        [Description("Specify if content shall be automatically refreshed when entitycollection, service, flags etc are changed.")]
        public bool AutoRefresh
        {
            get { return autoRefresh; }
            set
            {
                autoRefresh = value;
                if (autoRefresh)
                {
                    Refresh();
                }
            }
        }

        [Category("CRM")]
        [DefaultValue(false)]
        [Description("True to show friendly names, False to show logical names and guid etc.")]
        public bool ShowFriendlyNames
        {
            get { return showFriendlyNames; }
            set
            {
                showFriendlyNames = value;
                if (autoRefresh)
                {
                    Refresh();
                }
            }
        }

        [Category("CRM")]
        [DefaultValue(true)]
        [Description("Set this to show the id of each record first in the grid.")]
        public bool ShowIdColumn
        {
            get { return showIdColumn; }
            set
            {
                showIdColumn = value;
                if (autoRefresh)
                {
                    Refresh();
                }
            }
        }

        [Category("CRM")]
        [DefaultValue(true)]
        [Description("Set this to display a counter column first in the grid.")]
        public bool ShowIndexColumn
        {
            get { return showIndexColumn; }
            set
            {
                showIndexColumn = value;
                if (autoRefresh)
                {
                    Refresh();
                }
            }
        }

        [Category("CRM")]
        [DefaultValue(false)]
        [Description("Set this to give EntityReference cells a clickable appearance.")]
        public bool EntityReferenceClickable
        {
            get { return entityReferenceClickable; }
            set { entityReferenceClickable = value; }
        }

        /// <summary>
        /// Refresh the contents of the gridview based on associated Entities and flags
        /// </summary>
        public override void Refresh()
        {
            if (entityCollection != null)
            {
                var cols = GetTableColumns(entityCollection);
                var data = GetDataTable(entityCollection, cols);
                BindData(data);
            }
            base.Refresh();
        }

        [Category("CRM")]
        public event CRMRecordEventHandler RecordClick;

        [Category("CRM")]
        public event CRMRecordEventHandler RecordDoubleClick;

        private void HandleRecordClick(object sender, DataGridViewCellEventArgs e)
        {
            OnRecordClick(GetCRMRecordEventArgs(e));
        }

        private void HandleRecordDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex >= 0 && e.RowIndex >= 0)
            {
                OnRecordDoubleClick(GetCRMRecordEventArgs(e));
            }
        }

        private void HandleCellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (entityReferenceClickable)
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0)
                {
                    return;
                }
                var entity = GetRecordFromCellEvent(e);
                var row = Rows[e.RowIndex];
                var col = Columns[e.ColumnIndex];
                if (!entity.Contains(col.Name))
                {
                    return;
                }
                var value = entity[col.Name];
                if (value is EntityReference)
                {
                    var font = new Font(Font, FontStyle.Underline);
                    var cell = row.Cells[e.ColumnIndex];
                    cell.Style.Font = font;
                    Cursor = Cursors.Hand;
                }
            }
        }

        private void HandleCellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            if (entityReferenceClickable)
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0)
                {
                    return;
                }
                var row = Rows[e.RowIndex];
                var cell = row.Cells[e.ColumnIndex];
                var font = new Font(Font, FontStyle.Regular);
                cell.Style.Font = font;
                Cursor = Cursors.Default;
            }
        }

        protected virtual void OnRecordClick(CRMRecordEventArgs e)
        {
            var handler = RecordClick;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnRecordDoubleClick(CRMRecordEventArgs e)
        {
            var handler = RecordDoubleClick;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private CRMRecordEventArgs GetCRMRecordEventArgs(DataGridViewCellEventArgs e)
        {
            Entity entity = GetRecordFromCellEvent(e);
            var attribute = e.ColumnIndex >= 0 ? Columns[e.ColumnIndex].Name : string.Empty;
            var args = new CRMRecordEventArgs(entity, attribute);
            return args;
        }

        private Entity GetRecordFromCellEvent(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1)
            {
                return null;
            }
            var rowno = e.RowIndex;
            var row = Rows[rowno];
            var entity = row.Cells["#entity"].Value as Entity;
            return entity;
        }

        private List<DataColumn> GetTableColumns(EntityCollection entities)
        {
            var columns = new List<DataColumn>();
            if (!designedColumnsDetermined)
            {
                designedColumns = Columns.Count > 0;
                designedColumnsDetermined = true;
            }
            if (designedColumns)
            {
                PopulateColumnsFromDesign(entities, columns);
            }
            else
            {
                Columns.Clear();
                columns.Add(new DataColumn("#no", typeof(int)) { Caption = "#", AutoIncrement = true, AutoIncrementSeed = 1 });
                columns.Add(new DataColumn("#id", typeof(Guid)) { Caption = "Id" });
                PopulateColumnsFromEntities(entities, columns);
            }
            columns.Add(new DataColumn("#entity", typeof(Entity)));
            return columns;
        }

        private void PopulateColumnsFromDesign(EntityCollection entities, List<DataColumn> columns)
        {
            foreach (DataGridViewColumn viewcol in Columns)
            {
                if (string.IsNullOrEmpty(viewcol.DataPropertyName))
                {
                    viewcol.DataPropertyName = viewcol.Name;
                }
                var attribute = viewcol.DataPropertyName;
                var value = GetFirstValueForAttribute(entities, attribute);
                var type = !showFriendlyNames && value != null ? value.GetType() : typeof(string);
                var dataColumn = new DataColumn(attribute, type);
                dataColumn.Caption = viewcol.HeaderText;
                var meta = MetadataHelper.GetAttribute(organizationService, entities.EntityName, attribute);
                dataColumn.ExtendedProperties.Add("Metadata", meta);
                dataColumn.ExtendedProperties.Add("OriginalType", value != null ? value.GetType() : null);
                columns.Add(dataColumn);
            }
        }

        private void PopulateColumnsFromEntities(EntityCollection entities, List<DataColumn> columns)
        {
            var addedColumns = new List<string>();
            foreach (var entity in entities.Entities)
            {
                foreach (var attribute in entity.Attributes.Keys)
                {
                    if (entity[attribute] == null)
                    {
                        continue;
                    }
                    if (entity[attribute] is Guid && (Guid)entity[attribute] == entity.Id)
                    {
                        continue;
                    }
                    if (addedColumns.Contains(attribute))
                    {
                        continue;
                    }

                    var meta = MetadataHelper.GetAttribute(organizationService, entities.EntityName, attribute);
                    var value = EntitySerializer.AttributeToBaseType(entity[attribute]);
                    var type = showFriendlyNames ? typeof(string) : value.GetType();
                    var dataColumn = new DataColumn(attribute, type);
                    dataColumn.Caption =
                        showFriendlyNames &&
                        meta != null &&
                        meta.DisplayName != null &&
                        meta.DisplayName.UserLocalizedLabel != null ? meta.DisplayName.UserLocalizedLabel.Label : attribute;
                    dataColumn.ExtendedProperties.Add("Metadata", meta);
                    dataColumn.ExtendedProperties.Add("OriginalType", value.GetType());
                    columns.Add(dataColumn);
                    addedColumns.Add(attribute);
                }
            }
        }

        private object GetFirstValueForAttribute(EntityCollection entities, string attribute)
        {
            foreach (var entity in entities.Entities)
            {
                if (entity.Contains(attribute) && entity[attribute] != null)
                {
                    return entity[attribute];
                }
            }
            return null;
        }

        private DataTable GetDataTable(EntityCollection entities, List<DataColumn> columns)
        {
            var dTable = new DataTable();
            dTable.Columns.AddRange(columns.ToArray());
            foreach (var entity in entities.Entities)
            {
                var dRow = dTable.NewRow();
                foreach (DataColumn column in dTable.Columns)
                {
                    var col = column.ColumnName;
                    try
                    {
                        object value = null;
                        if (col == "#no")
                        {   // Sequence column
                            continue;
                        }
                        else if (col == "#id")
                        {
                            value = entity.Id;
                        }
                        else if (col == "#entity")
                        {
                            value = entity;
                        }
                        else if (entity.Contains(col) && entity[col] != null)
                        {
                            value = entity[col];
                            if (showFriendlyNames)
                            {
                                if (column.ExtendedProperties.ContainsKey("Metadata"))
                                {
                                    value = EntitySerializer.AttributeToString(value, column.ExtendedProperties["Metadata"] as AttributeMetadata);
                                }
                                else
                                {
                                    value = EntitySerializer.AttributeToBaseType(value).ToString();
                                }
                            }
                            else
                            {
                                value = EntitySerializer.AttributeToBaseType(value);
                            }
                        }
                        if (value == null)
                        {
                            value = DBNull.Value;
                        }
                        dRow[column] = value;
                    }
                    catch
                    {
                        MessageBox.Show("Attribute " + col + " failed, value: " + entity[col].ToString());
                    }
                }
                dTable.Rows.Add(dRow);
            }
            return dTable;
        }

        private void BindData(DataTable dTable)
        {
            SuspendLayout();
            base.DataSource = dTable;
            foreach (DataGridViewColumn col in Columns)
            {
                var datacolumn = dTable.Columns[col.DataPropertyName];
                col.HeaderText = datacolumn.Caption;
                var type = datacolumn.DataType;
                if (datacolumn.ExtendedProperties.ContainsKey("OriginalType"))
                {
                    type = datacolumn.ExtendedProperties["OriginalType"] as Type;
                }
                if (type == typeof(int) || type == typeof(decimal))
                {
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (datacolumn.ColumnName == "#no")
                {
                    col.Visible = showIndexColumn;
                }
                if (datacolumn.ColumnName == "#id")
                {
                    col.Visible = showIdColumn;
                }
                if (datacolumn.ColumnName == "#entity")
                {
                    col.Visible = false;
                }
            }
            AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCellsExceptHeader);
            ResumeLayout();
        }
    }

    public delegate void CRMRecordEventHandler(object sender, CRMRecordEventArgs e);
}
