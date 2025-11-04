using ServiceStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using System.Web.Helpers;
using System.Web.Routing;
using System.Web.Script.Serialization;
using System.Web.Security;
using System.Web.Services;
using System.Web.UI.WebControls;
using Telerik.Microsoft.Practices.EnterpriseLibrary.Security;
using Telerik.OpenAccess;
using Telerik.Sitefinity.Abstractions;
using Telerik.Sitefinity.Data;
using Telerik.Sitefinity.Data.ContentLinks;
using Telerik.Sitefinity.Data.Linq.Dynamic;
using Telerik.Sitefinity.Data.Metadata;
using Telerik.Sitefinity.Data.Utilities.Exporters;
using Telerik.Sitefinity.Forms.Model;
using Telerik.Sitefinity.Lifecycle;
using Telerik.Sitefinity.Localization;
using Telerik.Sitefinity.Model;
using Telerik.Sitefinity.Modules;
using Telerik.Sitefinity.Modules.Forms;
using Telerik.Sitefinity.Modules.Forms.Web;
using Telerik.Sitefinity.Modules.Pages;
using Telerik.Sitefinity.Multisite;
using Telerik.Sitefinity.Pages.Model;
using Telerik.Sitefinity.Security.Model;
using Telerik.Sitefinity.Security;
using Telerik.Sitefinity.Services;
using Telerik.Sitefinity.Web;
using Telerik.Web.Data.Extensions;
using Telerik.Web.UI;
using ContentLifecycleStatus = Telerik.Sitefinity.GenericContent.Model.ContentLifecycleStatus;

namespace SitefinityWebApp
{
    public partial class PageAnalysis : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SystemManager.RegisterRoute("Analyzer", new Route("Sitefinity/export-Analyzer", ObjectFactory.Resolve<AnalyzerExportRouteHandler>()), "Pages", false);
            this.runTemplateAnalysis.Click += RunTemplateAnalysis_Click;
            this.runPageAnalysis.Click += RunPageAnalysis_Click;
            this.GetWidgetsInfo.Click += GetWidgetsInfo_Click;
            this.runFormsAnalysis.Click += RunFormsAnalysis_Click;

            if (Request.Form["LanguageDropDown"] != null)
            {
                SystemManager.CurrentContext.Culture = CultureInfo.GetCultureInfo(Request.Form["LanguageDropDown"]);
            }

            // Update visibility of btnSearchClear based on the search value
            var clearButton = this.searchSection.FindControl("btnSearchClear") as Button;
            if (clearButton != null)
            {
                if (!string.IsNullOrWhiteSpace(this.txtSearch.Value))
                {
                    clearButton.Style["display"] = "block";

                }
                else
                {
                    clearButton.Style["display"] = "none";
                }
            }

            if (!Page.IsPostBack)
            {
                var multisiteContext = SystemManager.CurrentContext as MultisiteContext;
                var allSites = multisiteContext.GetSites().ToList();
                var defaultSite = allSites.FirstOrDefault(s => s.IsDefault);
                this.SitesDropDown.DataSource = allSites;
                this.SitesDropDown.SelectedIndex = allSites.FindIndex(s => s.Id == defaultSite.Id);
                this.SitesDropDown.DataBind();
                this.siteSelection.SelectedIndex = 1;

                var allSystemLanguages = SystemManager.CurrentContext.SystemCultures;
                //var allLanguagesForCurrentSite = SystemManager.CurrentContext.CurrentSite.Cultures;                
                if (allSystemLanguages.Count() > 1)
                {
                    var defaultCulture = SystemManager.CurrentContext.CurrentSite.DefaultCulture;
                    this.LanguageDropDown.DataSource = allSystemLanguages;
                    this.LanguageDropDown.SelectedIndex = allSystemLanguages.ToList<CultureInfo>().FindIndex(c => c.Name == defaultCulture.Name);
                    this.LanguageDropDown.DataBind();
                }
                else
                {
                    this.languageSection.Visible = false;
                }

                var siteMapRootNodeId = defaultSite?.SiteMapRootNodeId;
                this.SetUpTemplatesScreen(siteMapRootNodeId, this.siteSelection.SelectedValue);

                MetadataManager metadataManager = MetadataManager.GetManager();
                var sitefinityVersionModel = metadataManager.GetModuleVersion("Sitefinity");
                if (sitefinityVersionModel.Version < new Version(15, 2, 8428))
                {
                    this.PageListControl.Columns[this.PageListControl.Columns.Count - 1].Visible = false;
                    this.TemplateListControl.Columns[this.TemplateListControl.Columns.Count - 1].Visible = false;
                    this.TemplateHierarchyListControl.Columns[this.TemplateHierarchyListControl.Columns.Count - 1].Visible = false;
                    this.SinglePageListControl.Columns[this.SinglePageListControl.Columns.Count - 1].Visible = false;
                }
            }
        }

        private void RunFormsAnalysis_Click(object sender, EventArgs e)
        {
            var siteMapRootNodeId = Request.Form["SitesDropDown"] ?? this.SitesDropDown.SelectedValue;
            var siteSelection = Request.Form["siteSelection"] ?? this.siteSelection.SelectedValue;
            Guid? siteId = string.IsNullOrEmpty(siteMapRootNodeId) ? Guid.Empty : Guid.Parse(siteMapRootNodeId);
            var screenName = "Forms";
            var selectedSiteId = Request.Form["SitesDropDown"];

            this.reportHeader.Text = screenName;

            if (sender != null)
                this.txtSearch.Value = string.Empty;

            var formsReport = new FormsReport();
            formsReport.PopulateFormsInfo(siteId, siteSelection, filterArgs: new FilterArgs()
            {
                Take = this.formsList.PageSize,
                SearchExpression = this.txtSearch.Value
            });
            this.formsInfoLiteral.Text = $"<b>Forms count</b> ASP.NET Core: {formsReport.NetCoreFormsCount}, Next.js: {formsReport.NextJsFormsCount}, MVC: {formsReport.MvcFormsCount}, " +
    $"Web Forms: {formsReport.WebFormsFormsCount}";

            this.formsList.DataSource = formsReport.FormsInfo;
            this.formsList.PageIndex = 0;
            this.formsList.VirtualItemCount = formsReport.TotalFormsCount;
            this.formsList.AllowSorting = true;
            this.formsList.AllowPaging = true;
            this.formsList.RowCommand += Form_RowCommand;
            this.formsList.DataBind();

            this.ShowHideGrids(screenName, false);
        }

        public void RunTemplateAnalysis_Click(object sender, EventArgs e)
        {
            if (sender != null)
                this.txtSearch.Value = string.Empty;
            var siteMapRootNodeId = Request.Form["SitesDropDown"] ?? this.SitesDropDown.SelectedValue;
            var siteSelection = Request.Form["siteSelection"] ?? this.siteSelection.SelectedValue;
            Guid? siteId = string.IsNullOrEmpty(siteMapRootNodeId) ? Guid.Empty : Guid.Parse(siteMapRootNodeId);
            this.ShowHideGrids("Templates", false);

            this.SetUpTemplatesScreen(siteId, siteSelection);
        }

        protected void BackButton_Click(object sender, EventArgs e)
        {
            var screenName = this.backButton.Attributes["sf-screenViewName"];
            this.ShowHideGrids(screenName, false);
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            var clearButton = this.searchSection.FindControl("btnSearchClear") as Button;

            if ((sender as Button).ID == "btnSearchClear")
            {
                this.txtSearch.Value = string.Empty;
                if (clearButton != null)
                {
                    clearButton.Style["display"] = "none";
                }
            }
            else if ((sender as Button).ID == "btnSearch")
            {
                if (!string.IsNullOrWhiteSpace(this.txtSearch.Value) && clearButton != null)
                {
                    clearButton.Style["display"] = "block";
                }
            }

            this.refreshGrids(null, null);
        }

        private void ShowHideGrids(string screenName, bool isDetails)
        {
            var allGrids = new List<GridView>() { this.PageListControl, this.SinglePageListControl, this.TemplateListControl, this.TemplateHierarchyListControl, this.AllWidgetsControl, this.SingleWidgetListControl, this.formsList, this.singleFormList, this.fieldsList };
            allGrids.ForEach(grid => { grid.Visible = false; });

            var allScreenSpecificElements = new List<System.Web.UI.Control>() { this.backButton, this.siteSelection, this.languageSection, this.pagesInfoAlert, this.formsInfoAlert, this.templatesInfoAlert,
            this.allWidgetsGridTitle, this.allTemplatesGridTitle, this.allPagesGridTitle, this.fieldsInfoTitle, this.allFormsTitle };
            allScreenSpecificElements.ForEach(element => { element.Visible = false; });

            this.backButton.Attributes.Add("sf-screenViewName", screenName);
            this.txtSearch.Attributes["placeholder"] = screenName == "Widgets" ? "Search by name" : "Search by title";
            if (!isDetails)
            {
                this.exportSection.Visible = true;
                this.searchSection.Visible = true;
                this.reportHeader.Text = screenName;
                this.TemplateListControl.Attributes["WidgetKey"] = string.Empty;
                this.TemplateListControl.Attributes["FormKey"] = string.Empty;
                this.TemplateListControl.Attributes["PageId"] = string.Empty;
                this.TemplateListControl.AllowSorting = true;
                this.PageListControl.Attributes["WidgetKey"] = string.Empty;
                this.PageListControl.Attributes["FormKey"] = string.Empty;
                this.PageListControl.Attributes["TemplateId"] = string.Empty;
                this.PageListControl.AllowSorting = true;
                this.AllWidgetsControl.Attributes["PageId"] = string.Empty;
                this.AllWidgetsControl.Attributes["TemplateId"] = string.Empty;
                switch (screenName)
                {
                    case "Templates":
                        this.TemplateListControl.Visible = true;
                        this.siteSelection.Visible = true;
                        this.languageSection.Visible = SystemManager.CurrentContext.SystemCultures.Count() > 1;
                        this.templatesInfoAlert.Visible = true;
                        break;
                    case "Pages":
                        this.PageListControl.Visible = true;
                        this.pagesInfoAlert.Visible = true;
                        this.languageSection.Visible = SystemManager.CurrentContext.SystemCultures.Count() > 1;
                        break;
                    case "Forms":
                        this.formsInfoAlert.Visible = true;
                        this.siteSelection.Visible = true;
                        this.formsList.AllowPaging = true;
                        this.formsList.AllowSorting = true;
                        this.formsList.Visible = true;
                        this.languageSection.Visible = SystemManager.CurrentContext.SystemCultures.Count() > 1;
                        break;
                    case "Widgets":
                        this.AllWidgetsControl.Visible = true;
                        break;
                }
            }
            else
            {
                this.exportSection.Visible = false;
                this.searchSection.Visible = false;
                this.backButton.Text = "< Back to all " + screenName.ToLower();
                this.backButton.Visible = true;
                switch (screenName)
                {
                    case "Templates":
                        this.TemplateHierarchyListControl.Visible = true;
                        this.PageListControl.Visible = true;
                        this.AllWidgetsControl.Visible = true;
                        this.allWidgetsGridTitle.Visible = true;
                        this.allPagesGridTitle.Visible = true;
                        this.allFormsTitle.Visible = true;
                        this.formsList.AllowPaging = false;
                        this.formsList.AllowSorting = false;
                        this.formsList.Visible = true;
                        break;
                    case "Pages":
                        this.SinglePageListControl.Visible = true;
                        this.TemplateListControl.Visible = true;
                        this.TemplateListControl.AllowSorting = false;
                        this.AllWidgetsControl.Visible = true;
                        this.allWidgetsGridTitle.Visible = true;
                        this.allFormsTitle.Visible = true;
                        this.formsList.AllowPaging = false;
                        this.formsList.AllowSorting = false;
                        this.formsList.Visible = true;
                        this.allTemplatesGridTitle.Visible = true;
                        break;
                    case "Widgets":
                        this.SingleWidgetListControl.Visible = true;
                        this.PageListControl.Visible = true;
                        this.TemplateListControl.Visible = true;
                        this.TemplateListControl.AllowSorting = true;
                        this.allPagesGridTitle.Visible = true;
                        this.allTemplatesGridTitle.Visible = true;
                        break;
                    case "Forms":
                        this.singleFormList.Visible = true;
                        this.PageListControl.Visible = true;
                        this.TemplateListControl.Visible = true;
                        this.TemplateListControl.AllowSorting = true;
                        this.allPagesGridTitle.Visible = true;
                        this.allTemplatesGridTitle.Visible = true;
                        this.fieldsList.Visible = true;
                        this.fieldsInfoTitle.Visible = true;
                        break;
                }
            }
        }

        private void SetUpTemplatesScreen(Guid? siteMapRootNodeId, string siteSelection)
        {
            var screenName = "Templates";
            this.reportHeader.Text = screenName;

            var templatesReport = new TemplatesReport();
            var filterArgs = new FilterArgs() { Take = this.TemplateListControl.PageSize, SortDirection = "DESC", SearchExpression = this.txtSearch.Value };
            templatesReport.PopulateTemplatesInfo(siteMapRootNodeId, siteSelection, filterArgs);

            this.templatesInfoLiteral.Text = $"<b>Templates count</b> ASP.NET Core: {templatesReport.NetCoreTemplatesCount}, Next.js: {templatesReport.NextJsTemplatesCount}, MVC: {templatesReport.MvcTemplatesCount}, " +
                $"Web Forms: {templatesReport.WebFormsTemplatesCount}, " +
            $"Hybrid: {templatesReport.HybridTemplatesCount}";

            this.TemplateListControl.VirtualItemCount = templatesReport.TotalTemplatesCount;
            this.TemplateListControl.Columns[7].Visible = true;
            this.TemplateListControl.PageIndex = 0;
            this.TemplateListControl.DataSource = templatesReport.TemplatesInfo;
            this.TemplateListControl.DataBind();
        }

        protected void RunPageAnalysis_Click(object sender, EventArgs e)
        {
            var screenName = "Pages";
            var selectedSiteId = Request.Form["SitesDropDown"];
            if (sender != null)
                this.txtSearch.Value = string.Empty;
            this.reportHeader.Text = screenName;
            var pagesReport = new PagesReport();
            var filterArgs = new FilterArgs() { Take = this.PageListControl.PageSize, SearchExpression = (this.txtSearch.Value) };
            pagesReport.PopulatePagesInfo(selectedSiteId, filterArgs, null);

            this.pagesInfo.Text = $"<b>Pages count:</b> Pages: {pagesReport.StandardPagesCount}, Translations: {pagesReport.TotalPageDataCount}";

            this.PageListControl.VirtualItemCount = pagesReport.FilteredPageDataCount;
            this.PageListControl.PageIndex = 0;
            this.PageListControl.Columns[7].Visible = true;
            this.PageListControl.DataSource = pagesReport.PagesInfo;
            this.PageListControl.DataBind();

            this.ShowHideGrids(screenName, false);
        }

        protected void GetWidgetsInfo_Click(object sender, EventArgs e)
        {
            var screenName = "Widgets";
            var selectedSiteId = Request.Form["SitesDropDown"];

            this.reportHeader.Text = screenName;

            if (sender != null)
                this.txtSearch.Value = string.Empty;

            var widgetReport = new WidgetReport();
            widgetReport.PopulateBasicWidgetInformation(selectedSiteId, filterArgs: new FilterArgs()
            {
                Take = this.AllWidgetsControl.PageSize,
                SearchExpression = this.txtSearch.Value
            });

            this.AllWidgetsControl.DataSource = widgetReport.AllWidgets;
            this.AllWidgetsControl.PageIndex = 0;
            this.AllWidgetsControl.VirtualItemCount = widgetReport.TotalDistinctWidgetsCount;
            this.AllWidgetsControl.RowCommand += WidgetDetails_RowCommand;
            this.AllWidgetsControl.DataBind();

            this.ShowHideGrids(screenName, false);
        }

        protected void AllWidgets_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            var gridView = sender as GridView;
            var pageSize = gridView.PageSize;
            var selectedSiteId = Request.Form["SitesDropDown"];

            var widgetReport = new WidgetReport();
            var filterArgs = new FilterArgs()
            {
                Skip = e.NewPageIndex * pageSize,
                Take = pageSize,
                SortExpression = this.AllWidgetsControl.Attributes["SortExpression"],
                SortDirection = this.AllWidgetsControl.Attributes["SortDirection"]
            };
            widgetReport.PopulateBasicWidgetInformation(selectedSiteId, filterArgs: filterArgs);

            this.AllWidgetsControl.DataSource = widgetReport.AllWidgets;
            this.AllWidgetsControl.PageIndex = e.NewPageIndex;
            this.AllWidgetsControl.DataBind();

            AddSortedColumnIndicator(gridView, this.AllWidgetsControl.Attributes["SortExpression"], this.AllWidgetsControl.Attributes["SortDirection"]);
        }

        protected void Pages_Sorting(object sender, GridViewSortEventArgs e)
        {
            var selectedSiteId = Request.Form["SitesDropDown"];
            var gridView = sender as GridView;
            var pagesReport = new PagesReport();

            var sortDirection = this.PageListControl.Attributes["SortDirection"] ?? "ASC";
            if (e.SortExpression == this.PageListControl.Attributes["SortExpression"])
            {
                sortDirection = (sortDirection == "ASC") ? "DESC" : "ASC";
            }

            var templateIdString = this.PageListControl.Attributes["TemplateId"];
            Guid? nullableTemplateId = null;
            if (Guid.TryParse(templateIdString, out Guid templateId))
                nullableTemplateId = templateId;
            var widgetKey = this.PageListControl.Attributes["WidgetKey"];
            var formKey = this.PageListControl.Attributes["FormKey"];
            var widgetFramework = this.PageListControl.Attributes["WidgetFramework"];

            var filterArgs = new FilterArgs()
            {
                Take = gridView.PageSize,
                SortExpression = e.SortExpression,
                SortDirection = sortDirection,
            };
            if (string.IsNullOrEmpty(widgetKey) && string.IsNullOrEmpty(formKey)) // is in main pages screen or in template details screen
            {
                filterArgs.SearchExpression = this.txtSearch.Value;
                pagesReport.PopulatePagesInfo(selectedSiteId, filterArgs, nullableTemplateId);
                this.PageListControl.VirtualItemCount = pagesReport.FilteredPageDataCount;
                this.PageListControl.DataSource = pagesReport.PagesInfo;
            }
            else if (!string.IsNullOrEmpty(widgetKey)) // is in details widget screen
            {
                var pagesWithWidget = WidgetReport.GetPagesWithWidget(widgetKey, widgetFramework.ToLower() == "mvc", out int pagesCount, filterArgs, selectedSiteId);
                this.PageListControl.VirtualItemCount = pagesCount;
                this.PageListControl.DataSource = pagesWithWidget;
            }
            else
            {
                var pagesWithWidget = WidgetReport.GetPagesWithForm(formKey, widgetFramework.ToLower(), out int pagesCount, filterArgs);
                this.PageListControl.VirtualItemCount = pagesCount;
                this.PageListControl.DataSource = pagesWithWidget;
            }

            this.PageListControl.Attributes["SortExpression"] = e.SortExpression;
            this.PageListControl.Attributes["SortDirection"] = sortDirection;
            this.PageListControl.PageIndex = 0;
            this.PageListControl.DataBind();


            AddSortedColumnIndicator(gridView, e.SortExpression, sortDirection);
        }

        protected void Pages_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            var selectedSiteId = Request.Form["SitesDropDown"];
            var gridView = sender as GridView;
            var pagesReport = new PagesReport();
            var pageSize = gridView.PageSize;
            var skip = e.NewPageIndex * pageSize;
            var sortExpression = this.PageListControl.Attributes["SortExpression"];
            var sortDirection = this.PageListControl.Attributes["SortDirection"];
            var templateIdString = this.PageListControl.Attributes["TemplateId"];
            Guid? nullableTemplateId = null;
            if (Guid.TryParse(templateIdString, out Guid templateId))
                nullableTemplateId = templateId;
            var widgetKey = this.PageListControl.Attributes["WidgetKey"];
            var formKey = this.PageListControl.Attributes["FormKey"];
            var widgetFramework = this.PageListControl.Attributes["WidgetFramework"];

            if (string.IsNullOrEmpty(widgetKey) && string.IsNullOrEmpty(formKey))
            {
                var filterArgs = new FilterArgs() { Skip = skip, Take = pageSize, SortDirection = sortDirection, SortExpression = sortExpression, SearchExpression = this.txtSearch.Value };
                pagesReport.PopulatePagesInfo(selectedSiteId, filterArgs, nullableTemplateId);
                this.PageListControl.VirtualItemCount = pagesReport.FilteredPageDataCount;
                this.PageListControl.DataSource = pagesReport.PagesInfo;
            }
            else if (!string.IsNullOrEmpty(widgetKey)) //in widget details screen
            {
                var filterArgs = new FilterArgs() { Skip = skip, Take = pageSize, SortDirection = sortDirection, SortExpression = sortExpression };
                var pagesWithWidget = WidgetReport.GetPagesWithWidget(widgetKey, widgetFramework.ToLower() == "mvc", out int pagesCount, filterArgs, selectedSiteId);
                this.PageListControl.VirtualItemCount = pagesCount;
                this.PageListControl.DataSource = pagesWithWidget;
            }
            else //in forms details screen
            {
                var filterArgs = new FilterArgs() { Skip = skip, Take = pageSize, SortDirection = sortDirection, SortExpression = sortExpression };
                var pagesWithWidget = WidgetReport.GetPagesWithForm(formKey, widgetFramework.ToLower(), out int pagesCount, filterArgs);
                this.PageListControl.VirtualItemCount = pagesCount;
                this.PageListControl.DataSource = pagesWithWidget;
            }

            this.PageListControl.PageIndex = e.NewPageIndex;
            this.PageListControl.DataBind();

            AddSortedColumnIndicator(gridView, sortExpression, sortDirection);
        }

        protected void Templates_Sorting(object sender, GridViewSortEventArgs e)
        {
            var selectedSiteId = Request.Form["SitesDropDown"];
            var siteSelection = Request.Form["siteSelection"];

            Guid? siteId = string.IsNullOrEmpty(selectedSiteId) ? Guid.Empty : Guid.Parse(selectedSiteId);
            var gridView = sender as GridView;

            var templatesReport = new TemplatesReport();
            var sortDirection = this.TemplateListControl.Attributes["SortDirection"] ?? "ASC";
            if (e.SortExpression == this.TemplateListControl.Attributes["SortExpression"])
            {
                sortDirection = (sortDirection == "ASC") ? "DESC" : "ASC";
            }

            var widgetKey = this.TemplateListControl.Attributes["WidgetKey"];
            var formKey = this.TemplateListControl.Attributes["FormKey"];
            var widgetFramework = this.TemplateListControl.Attributes["WidgetFramework"];
            var filterArgs = new FilterArgs() { SortExpression = e.SortExpression, SortDirection = sortDirection, Take = gridView.PageSize };
            if (string.IsNullOrEmpty(widgetKey) && string.IsNullOrEmpty(formKey))
            {
                filterArgs.SearchExpression = this.txtSearch.Value;
                templatesReport.PopulateTemplatesInfo(siteId, siteSelection, filterArgs);
                this.TemplateListControl.VirtualItemCount = templatesReport.TotalTemplatesCount;
                this.TemplateListControl.DataSource = templatesReport.TemplatesInfo;
            }
            else if (!string.IsNullOrEmpty(widgetKey))
            {
                var templatesWithWidget = WidgetReport.GetTemplatesWithWidget(widgetKey, widgetFramework.ToLower() == "mvc", out int pagesCount, filterArgs);
                this.TemplateListControl.VirtualItemCount = pagesCount;
                this.TemplateListControl.DataSource = templatesWithWidget;
            }
            else // in forms details screen
            {
                var templatesWithWidget = WidgetReport.GetTemplatesWithForm(formKey, widgetFramework.ToLower(), out int pagesCount, filterArgs);
                this.TemplateListControl.VirtualItemCount = pagesCount;
                this.TemplateListControl.DataSource = templatesWithWidget;
            }

            this.TemplateListControl.Attributes["SortExpression"] = e.SortExpression;
            this.TemplateListControl.Attributes["SortDirection"] = sortDirection;
            this.TemplateListControl.PageIndex = 0;
            this.TemplateListControl.DataBind();

            AddSortedColumnIndicator(gridView, e.SortExpression, sortDirection);
        }

        protected void Templates_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            var selectedSiteId = Request.Form["SitesDropDown"];
            var siteSelection = Request.Form["siteSelection"];
            Guid? siteId = string.IsNullOrEmpty(selectedSiteId) ? Guid.Empty : Guid.Parse(selectedSiteId);
            var gridView = sender as GridView;
            var templatesReport = new TemplatesReport();
            var pageSize = gridView.PageSize;
            var skip = e.NewPageIndex * pageSize;
            var sortExpression = this.TemplateListControl.Attributes["SortExpression"];
            var sortDirection = this.TemplateListControl.Attributes["SortDirection"];
            var widgetKey = this.TemplateListControl.Attributes["WidgetKey"];
            var formKey = this.TemplateListControl.Attributes["FormKey"];
            var widgetFramework = this.TemplateListControl.Attributes["WidgetFramework"];
            var filterArgs = new FilterArgs() { Skip = skip, Take = pageSize, SortExpression = sortExpression, SortDirection = sortDirection };

            if (string.IsNullOrEmpty(widgetKey) && string.IsNullOrEmpty(formKey))
            {
                filterArgs.SearchExpression = this.txtSearch.Value;
                templatesReport.PopulateTemplatesInfo(siteId, siteSelection, filterArgs);
                this.TemplateListControl.VirtualItemCount = templatesReport.TotalTemplatesCount;
                this.TemplateListControl.DataSource = templatesReport.TemplatesInfo;
            }
            else if (!string.IsNullOrEmpty(widgetKey))
            {
                var templatesWithWidget = WidgetReport.GetTemplatesWithWidget(widgetKey, widgetFramework.ToLower() == "mvc", out int pagesCount, filterArgs);
                this.TemplateListControl.VirtualItemCount = pagesCount;
                this.TemplateListControl.DataSource = templatesWithWidget;
            }
            else //in forms details screen
            {
                var templatesWithWidget = WidgetReport.GetTemplatesWithForm(formKey, widgetFramework.ToLower(), out int pagesCount, filterArgs);
                this.TemplateListControl.VirtualItemCount = pagesCount;
                this.TemplateListControl.DataSource = templatesWithWidget;
            }

            this.TemplateListControl.PageIndex = e.NewPageIndex;
            this.TemplateListControl.DataBind();
            AddSortedColumnIndicator(gridView, sortExpression, sortDirection);
        }

        private static void AddSortedColumnIndicator(GridView gridView, string sortExpression, string sortDirection)
        {
            int iSortedColIdx = 0;
            foreach (DataControlField c in gridView.Columns)
            {
                if (c.SortExpression == sortExpression)
                {
                    gridView.HeaderRow.Cells[iSortedColIdx].CssClass = sortDirection.ToLower();
                    break;
                }
                iSortedColIdx++;
            }
        }

        protected void TemplateHierarchy_Click(object sender, GridViewCommandEventArgs gridArgs)
        {
            if (gridArgs.CommandName != "GetDetails")
                return;

            var selectedSiteId = Request.Form["SitesDropDown"];

            var arg = gridArgs.CommandArgument.ToString().Split(';');
            this.allPagesGridTitle.Text = "Pages using this template";
            this.allWidgetsGridTitle.Text = "Widgets used in this template";
            this.reportHeader.Text = $"'{arg[2]}' template info and hierarchy";
            var parentTemplateId = Guid.Parse(arg[0]);

            var pagesReport = new PagesReport();
            var pageFlterArgs = new FilterArgs() { Take = this.PageListControl.PageSize };
            pagesReport.PopulatePagesInfo(selectedSiteId, pageFlterArgs, parentTemplateId);
            this.PageListControl.Attributes["TemplateId"] = arg[0];
            this.PageListControl.Attributes["WidgetKey"] = string.Empty;
            this.PageListControl.Attributes["FormKey"] = string.Empty;
            this.PageListControl.VirtualItemCount = pagesReport.FilteredPageDataCount;
            this.PageListControl.PageIndex = 0;
            this.PageListControl.DataSource = pagesReport.PagesInfo;
            this.PageListControl.DataBind();

            var templateReport = new TemplatesReport();
            var templateHierarchy = templateReport.GetTemplateParentHierarchy(parentTemplateId);
            this.TemplateHierarchyListControl.DataSource = templateHierarchy;
            this.TemplateHierarchyListControl.VirtualItemCount = templateHierarchy.Count;
            this.TemplateHierarchyListControl.DataBind();

            var widgetReport = new WidgetReport();
            var widgetFilterArgs = new FilterArgs() { Take = this.AllWidgetsControl.PageSize };
            widgetReport.PopulateBasicWidgetInformation(selectedSiteId, parentTemplateId, null, WidgetLocationMode.TemplateOnly, widgetFilterArgs);

            this.AllWidgetsControl.Attributes["TemplateId"] = arg[0];
            this.AllWidgetsControl.Attributes["PageId"] = string.Empty;
            this.AllWidgetsControl.DataSource = widgetReport.AllWidgets;
            this.AllWidgetsControl.VirtualItemCount = widgetReport.TotalDistinctWidgetsCount;
            this.AllWidgetsControl.PageIndex = 0;
            this.AllWidgetsControl.RowCommand += WidgetDetails_RowCommand;
            this.AllWidgetsControl.DataBind();
            this.ShowHideGrids("Templates", true);

            this.allFormsTitle.Text = "Forms used in this template";
            this.formsList.DataSource = FormsReport.GetFormsInfo<TemplateControl>(parentTemplateId.ToString(), templateHierarchy.FirstOrDefault().Framework);
            this.formsList.DataBind();
        }

        protected void PageHierarchy_Click(object sender, GridViewCommandEventArgs gridArgs)
        {
            if (gridArgs.CommandName != "GetDetails")
                return;

            var selectedSiteId = Request.Form["SitesDropDown"];

            var arg = gridArgs.CommandArgument.ToString().Split(';');
            this.allTemplatesGridTitle.Text = "Templates used in this page";
            this.allWidgetsGridTitle.Text = "Widgets used in this pagе";
            this.allFormsTitle.Text = "Forms used in this page";
            this.reportHeader.Text = $"'{arg[2]}' page info";
            var pageId = Guid.Parse(arg[0]);

            var pageReport = new PagesReport();
            var selectedPageHierarchy = pageReport.GetPageInfo(pageId);
            this.SinglePageListControl.DataSource = selectedPageHierarchy;
            this.SinglePageListControl.VirtualItemCount = 1;
            this.SinglePageListControl.DataBind();

            Guid parentTemplateId;
            var manager = new PageManager();
            var template = manager.GetPageData(pageId).Template;
            parentTemplateId = template != null ? template.Id : Guid.Empty;

            var templateReport = new TemplatesReport();
            var templateHierarchy = templateReport.GetTemplateParentHierarchy(parentTemplateId);

            this.TemplateListControl.Attributes["PageId"] = arg[0];
            this.TemplateListControl.Attributes["WidgetKey"] = string.Empty;
            this.TemplateListControl.Attributes["FormKey"] = string.Empty;
            this.TemplateListControl.DataSource = templateHierarchy;
            this.TemplateListControl.PageIndex = 0;
            this.TemplateListControl.VirtualItemCount = templateHierarchy.Count;
            this.TemplateListControl.DataBind();

            var widgetReport = new WidgetReport();
            var filterArgs = new FilterArgs() { Take = this.AllWidgetsControl.PageSize };
            widgetReport.PopulateBasicWidgetInformation(selectedSiteId, null, pageId, WidgetLocationMode.PageOnly, filterArgs: filterArgs);

            this.AllWidgetsControl.Attributes["PageId"] = arg[0];
            this.AllWidgetsControl.Attributes["TemplateId"] = string.Empty;
            this.AllWidgetsControl.DataSource = widgetReport.AllWidgets;
            this.AllWidgetsControl.VirtualItemCount = widgetReport.TotalDistinctWidgetsCount;
            this.AllWidgetsControl.PageIndex = 0;
            this.AllWidgetsControl.RowCommand += WidgetDetails_RowCommand;
            this.AllWidgetsControl.DataBind();

            this.formsList.DataSource = FormsReport.GetFormsInfo<PageControl>(arg[0], selectedPageHierarchy.FirstOrDefault().Framework);
            this.formsList.DataBind();

            this.ShowHideGrids("Pages", true);
        }

        protected void WidgetDetails_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            var arg = e.CommandArgument.ToString().Split(';');
            if (arg.Length < 5)
                return;
            int.TryParse(arg[3], out int countOnPages);
            int.TryParse(arg[4], out int countOnTemplates);
            var selectedSiteId = Request.Form["SitesDropDown"];

            var templateFilterArgs = new FilterArgs() { Skip = 0, Take = this.TemplateListControl.PageSize };
            var templatesWithWidget = WidgetReport.GetTemplatesWithWidget(arg[0], arg[2].ToLower() == "mvc", out int templateCount, templateFilterArgs);
            this.TemplateListControl.Attributes["WidgetKey"] = arg[0];
            this.TemplateListControl.Attributes["FormKey"] = string.Empty;
            this.TemplateListControl.Attributes["WidgetFramework"] = arg[2];
            this.TemplateListControl.Attributes["PageId"] = string.Empty;
            this.TemplateListControl.DataSource = templatesWithWidget;
            this.TemplateListControl.PageIndex = 0;
            this.TemplateListControl.VirtualItemCount = templateCount;
            this.TemplateListControl.Columns[7].Visible = false;
            this.TemplateListControl.DataBind();

            var filterArgs = new FilterArgs() { Skip = 0, Take = this.PageListControl.PageSize };
            var pagesWithWidget = WidgetReport.GetPagesWithWidget(arg[0], arg[2].ToLower() == "mvc", out int pagesCount, filterArgs, selectedSiteId);
            this.PageListControl.Attributes["WidgetKey"] = arg[0];
            this.PageListControl.Attributes["FormKey"] = string.Empty;
            this.PageListControl.Attributes["WidgetFramework"] = arg[2];
            this.PageListControl.Attributes["TemplateId"] = string.Empty;
            this.PageListControl.DataSource = pagesWithWidget;
            this.PageListControl.VirtualItemCount = pagesCount;
            this.PageListControl.PageIndex = 0;
            this.PageListControl.Columns[7].Visible = false;
            this.PageListControl.DataBind();
            var singleWidgetInfo = new WidgetInfo(arg[0], arg[5], arg[2], countOnPages, countOnTemplates);
            this.SingleWidgetListControl.DataSource = new List<WidgetInfo>() { singleWidgetInfo };
            this.SingleWidgetListControl.DataBind();

            this.reportHeader.Text = $"'{singleWidgetInfo.Title}' widget info";
            this.allTemplatesGridTitle.Text = "Templates using this widget";
            this.allPagesGridTitle.Text = "Pages using this widget";

            this.ShowHideGrids("Widgets", true);
        }


        protected void Form_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            var arg = e.CommandArgument.ToString().Split(';');
            if (arg.Length < 6)
                return;
            int.TryParse(arg[4], out int countOnPages);
            int.TryParse(arg[5], out int countOnTemplates);
            var selectedSiteId = Request.Form["SitesDropDown"];
            this.fieldsInfoTitle.Text = "Fields used in this form";

            var templateFilterArgs = new FilterArgs() { Skip = 0, Take = this.TemplateListControl.PageSize };
            var templatesWithWidget = WidgetReport.GetTemplatesWithForm(arg[0], arg[3].ToLower(), out int templateCount, templateFilterArgs);
            this.TemplateListControl.Attributes["WidgetKey"] = string.Empty;
            this.TemplateListControl.Attributes["FormKey"] = arg[0];
            this.TemplateListControl.Attributes["WidgetFramework"] = arg[2];
            this.TemplateListControl.Attributes["PageId"] = string.Empty;
            this.TemplateListControl.DataSource = templatesWithWidget;
            this.TemplateListControl.PageIndex = 0;
            this.TemplateListControl.VirtualItemCount = templateCount;
            this.TemplateListControl.Columns[7].Visible = false;
            this.TemplateListControl.DataBind();

            var filterArgs = new FilterArgs() { Skip = 0, Take = this.PageListControl.PageSize };
            var pagesWithWidget = WidgetReport.GetPagesWithForm(arg[0], arg[3].ToLower(), out int pagesCount, filterArgs);
            this.PageListControl.Attributes["FormKey"] = arg[0];
            this.PageListControl.Attributes["WidgetKey"] = string.Empty;
            this.PageListControl.Attributes["WidgetFramework"] = arg[3];
            this.PageListControl.Attributes["TemplateId"] = string.Empty;
            this.PageListControl.DataSource = pagesWithWidget;
            this.PageListControl.VirtualItemCount = pagesCount;
            this.PageListControl.PageIndex = 0;
            this.PageListControl.Columns[7].Visible = false;
            this.PageListControl.DataBind();
            var singleFormInfo = new FormDescriptionInfo(arg[1], arg[2], arg[3], countOnPages, countOnTemplates);
            this.singleFormList.DataSource = new List<FormDescriptionInfo>() { singleFormInfo };
            this.singleFormList.DataBind();

            if (Guid.TryParse(arg[0], out Guid formId))
            {
                this.fieldsList.DataSource = FormsReport.GetFormControls(formId);
                this.fieldsList.DataBind();
            }

            this.reportHeader.Text = $"'{singleFormInfo.Title}' form info";
            this.allTemplatesGridTitle.Text = "Templates using this form";
            this.allPagesGridTitle.Text = "Pages using this form";

            this.ShowHideGrids("Forms", true);
        }

        protected void SitesDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.refreshGrids(null, null);
        }

        protected void LanguageDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            SystemManager.CurrentContext.Culture = CultureInfo.GetCultureInfo((sender as DropDownList).SelectedValue);
            this.refreshGrids(null, null);
        }

        protected void refreshGrids(object sender, EventArgs e)
        {
            var screenName = this.backButton.Attributes["sf-screenViewName"];

            if (screenName == "Templates" || string.IsNullOrEmpty(screenName))
                this.RunTemplateAnalysis_Click(null, null);
            else if (screenName == "Pages")
                this.RunPageAnalysis_Click(null, null);
            else if (screenName == "Widgets")
                this.GetWidgetsInfo_Click(null, null);
            else if (screenName == "Forms")
                this.RunFormsAnalysis_Click(null, null);
        }

        protected void AllWidgets_Sorting(object sender, GridViewSortEventArgs e)
        {
            var selectedSiteId = Request.Form["SitesDropDown"];
            var pageIdString = this.AllWidgetsControl.Attributes["PageId"];
            Guid? pageId = null;
            WidgetLocationMode widgetLocation = WidgetLocationMode.Both;
            if (!string.IsNullOrEmpty(pageIdString))
            {
                pageId = Guid.Parse(pageIdString);
                widgetLocation = WidgetLocationMode.PageOnly;
            }
            var templateIdString = this.AllWidgetsControl.Attributes["TemplateId"];
            Guid? templateId = null;
            if (!string.IsNullOrEmpty(templateIdString))
            {
                templateId = Guid.Parse(templateIdString);
                widgetLocation = WidgetLocationMode.TemplateOnly;
            }

            var sortDirection = this.AllWidgetsControl.Attributes["SortDirection"] ?? "ASC";
            if (e.SortExpression == this.AllWidgetsControl.Attributes["SortExpression"])
            {
                sortDirection = (sortDirection == "ASC") ? "DESC" : "ASC";
            }

            var widgetReport = new WidgetReport();
            var filterArgs = new FilterArgs()
            {
                Take = this.AllWidgetsControl.PageSize,
                SortExpression = e.SortExpression,
                SortDirection = sortDirection,
                SearchExpression = this.txtSearch.Value
            };
            widgetReport.PopulateBasicWidgetInformation(selectedSiteId, templateId, pageId, widgetLocation, filterArgs);

            this.AllWidgetsControl.Attributes["SortExpression"] = e.SortExpression;
            this.AllWidgetsControl.Attributes["SortDirection"] = sortDirection;
            this.AllWidgetsControl.DataSource = widgetReport.AllWidgets;
            this.AllWidgetsControl.PageIndex = 0;
            this.AllWidgetsControl.VirtualItemCount = widgetReport.TotalDistinctWidgetsCount;
            this.AllWidgetsControl.RowCommand += WidgetDetails_RowCommand;
            this.AllWidgetsControl.DataBind();


            AddSortedColumnIndicator(this.AllWidgetsControl, e.SortExpression, sortDirection);
        }

        protected void FormsList_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            var selectedSiteId = Request.Form["SitesDropDown"];
            var siteSelection = Request.Form["siteSelection"];
            Guid? siteId = string.IsNullOrEmpty(selectedSiteId) ? Guid.Empty : Guid.Parse(selectedSiteId);
            var gridView = sender as GridView;
            var formsReport = new FormsReport();
            var pageSize = gridView.PageSize;
            var skip = e.NewPageIndex * pageSize;
            var sortExpression = this.formsList.Attributes["SortExpression"];
            var sortDirection = this.formsList.Attributes["SortDirection"];
            var filterArgs = new FilterArgs() { Skip = skip, Take = pageSize, SortExpression = sortExpression, SortDirection = sortDirection };

            filterArgs.SearchExpression = this.txtSearch.Value;
            formsReport.PopulateFormsInfo(siteId, siteSelection, filterArgs);
            this.formsList.VirtualItemCount = formsReport.TotalFormsCount;
            this.formsList.DataSource = formsReport.FormsInfo;

            this.formsList.PageIndex = e.NewPageIndex;
            this.formsList.DataBind();

            AddSortedColumnIndicator(this.formsList, sortExpression, sortDirection);
        }

        protected void FormsList_Sorting(object sender, GridViewSortEventArgs e)
        {
            var siteMapRootNodeId = Request.Form["SitesDropDown"] ?? this.SitesDropDown.SelectedValue;
            var siteSelection = Request.Form["siteSelection"] ?? this.siteSelection.SelectedValue;
            Guid? siteId = string.IsNullOrEmpty(siteMapRootNodeId) ? Guid.Empty : Guid.Parse(siteMapRootNodeId);
            var sortDirection = this.formsList.Attributes["SortDirection"] ?? "ASC";
            if (e.SortExpression == this.formsList.Attributes["SortExpression"])
            {
                sortDirection = (sortDirection == "ASC") ? "DESC" : "ASC";
            }

            var formsReport = new FormsReport();
            var filterArgs = new FilterArgs()
            {
                Take = this.formsList.PageSize,
                SortExpression = e.SortExpression,
                SortDirection = sortDirection,
                SearchExpression = this.txtSearch.Value
            };
            formsReport.PopulateFormsInfo(siteId, siteSelection, filterArgs);

            this.formsList.Attributes["SortExpression"] = e.SortExpression;
            this.formsList.Attributes["SortDirection"] = sortDirection;
            this.formsList.DataSource = formsReport.FormsInfo;
            this.formsList.PageIndex = 0;
            this.formsList.VirtualItemCount = formsReport.TotalFormsCount;
            this.formsList.RowCommand += WidgetDetails_RowCommand;
            this.formsList.DataBind();

            AddSortedColumnIndicator(this.formsList, e.SortExpression, sortDirection);
        }
    }


    public class AnalyzerExportRouteHandler : IRouteHandler
    {
        /// <summary>
        /// Provides the object that processes the request.
        /// </summary>
        /// <param name="requestContext">An object that encapsulates information about the request.</param>
        /// <returns>An object that processes the request.</returns>
        public IHttpHandler GetHttpHandler(System.Web.Routing.RequestContext requestContext)
        {
            return new AnalyzerExportHttpHandler();
        }
    }

    public class AnalyzerExportHttpHandler : IHttpHandler
    {
        #region Properties


        #endregion

        #region IHttpHandler Implementation

        /// <summary>
        /// Enables processing of HTTP Web requests by a custom HttpHandler that implements the <see cref="T:System.Web.IHttpHandler"/> interface.
        /// </summary>
        /// <param name="context">An <see cref="T:System.Web.HttpContext"/> object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests.</param>
        public void ProcessRequest(HttpContext context)
        {
            ProcessRequest(new HttpContextWrapper(context));
        }

        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="context">The context.</param>
        public void ProcessRequest(HttpContextBase context)
        {
            this.InitializeParameters(context);

            var viewName = this.GetQueryStringValue(context, "viewName", true);
            var siteIdString = this.GetQueryStringValue(context, "siteId", true);
            var language = this.GetQueryStringValue(context, "language", false);
            var siteSelection = this.GetQueryStringValue(context, "siteSelection", false);
            Guid siteId = string.IsNullOrEmpty(siteIdString) ? Guid.Empty : Guid.Parse(siteIdString);
            var multisiteContext = SystemManager.CurrentContext as MultisiteContext;
            var siteName = multisiteContext.GetSiteBySiteMapRoot(siteId)?.Name ?? "all";
            SystemManager.CurrentHttpContext.Items["ExportingItemsTypeName"] = typeof(TemplateInfo).FullName;
            SystemManager.CurrentHttpContext.Items["SheetName"] = "templates";
            if (!string.IsNullOrEmpty(language))
                SystemManager.CurrentContext.Culture = CultureInfo.GetCultureInfo(language);

            var skip = 0;
            var take = 20;
            var filterArgs = new FilterArgs() { Skip = skip, Take = take, SortDirection = "DESC", Culture = language };
            var items = this.GetItems(viewName, siteId, siteSelection, filterArgs, out int totalCount);

            var exporter = new AnalyzerCsvExporter();
            var fileName = $"analyzer-{viewName}-{(siteSelection == "current" ? siteName : siteSelection)}{(string.IsNullOrEmpty(language) ? string.Empty : "-" + language)}";
            var invalidChars = Path.GetInvalidFileNameChars();
            exporter.FileName = new string(fileName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());

            exporter.ConfigureResponse(context.ApplicationInstance.Context.Response);
            do
            {
                exporter.ExportToStream(context.Response.OutputStream, items, context.ApplicationInstance.Context.Response.ContentEncoding);

                if (skip == 0)
                {
                    var prop = exporter.GetType().GetProperty("AddHeader", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(exporter, false);
                    }
                }
                skip += take;

                if (skip >= totalCount)
                    break;
                filterArgs = new FilterArgs() { Skip = skip, Take = take, SortDirection = "DESC", Culture = language };
                items = this.GetItems(viewName, siteId, siteSelection, filterArgs, out totalCount);
            }
            while (items.Any());
        }

        private IEnumerable<IDataItem> GetItems(string screenName, Guid siteId, string siteSelection, FilterArgs filterArgs, out int totalCount)
        {
            switch (screenName)
            {
                case "templates":
                    var templatesReport = new TemplatesReport();
                    templatesReport.PopulateTemplatesInfo(siteId, siteSelection, filterArgs);
                    totalCount = templatesReport.TotalTemplatesCount;
                    return templatesReport.TemplatesInfo.Cast<IDataItem>();
                case "pages":
                    var pagesReport = new PagesReport();
                    pagesReport.PopulatePagesInfo(siteId.ToString(), filterArgs);
                    totalCount = pagesReport.TotalPageDataCount;
                    return pagesReport.PagesInfo.Cast<IDataItem>();
                case "widgets":
                    var widgetReport = new WidgetReport();
                    widgetReport.PopulateBasicWidgetInformation(siteId.ToString(), filterArgs: filterArgs);
                    totalCount = widgetReport.TotalDistinctWidgetsCount;
                    return widgetReport.AllWidgets.Cast<IDataItem>();
                case "forms":
                    var formsReport = new FormsReport();
                    formsReport.PopulateFormsInfo(siteId, siteSelection, filterArgs);
                    totalCount = formsReport.TotalFormsCount;
                    return formsReport.FormsInfo.Cast<IDataItem>();
                default:
                    totalCount = 0;
                    return null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler"/> instance.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Web.IHttpHandler"/> instance is reusable; otherwise, false.
        /// </returns>
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region Private methods

        private void InitializeParameters(HttpContextBase context)
        {
            //this.FormName = this.GetQueryStringValue(context, "formName", true);
            //this.FileToExportFormat = this.GetQueryStringValue(context, "exportAs", true);
            //this.ExportingItemsTypeName = this.GetQueryStringValue(context, "itemType", true);
            //this.ProviderName = this.GetQueryStringValue(context, "provider", false);
        }

        private string GetQueryStringValue(HttpContextBase context, string queryStringParamName, bool required)
        {
            var queryStringParameter = context.Request.QueryString[queryStringParamName];
            if (String.IsNullOrEmpty(queryStringParameter) && required)
            {
                throw new ArgumentNullException(queryStringParamName);
            }
            return queryStringParameter;
        }
        #endregion
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    internal class SkipExportAttribute : Attribute
    {
        public SkipExportAttribute() { }
    }

    public class AnalyzerCsvExporter : CsvExporter
    {

        protected override PropertyDescriptorCollection GetItemProperties(object item)
        {
            var properties = base.GetItemProperties(item);
            var friendlyUserNameAttrType = typeof(SkipExportAttribute);
            var mutableProperties = properties.Cast<PropertyDescriptor>().ToList();

            var propsToExport = mutableProperties.Where(p => !(p.Attributes[friendlyUserNameAttrType] is SkipExportAttribute));

            return new PropertyDescriptorCollection(propsToExport.ToArray());
        }
    }

    public enum WidgetLocationMode
    {
        TemplateOnly,
        PageOnly,
        Both
    }

    public class WidgetReport
    {
        public IList<WidgetInfo> MvcWidgetsOnTemplates { get; set; }
        public IList<WidgetInfo> MvcWidgetsOnPages { get; set; }
        public IList<WidgetInfo> WebFormsWidgetsOnTemplates { get; set; }
        public IList<WidgetInfo> WebFormsWidgetsOnPages { get; set; }
        public ICollection<WidgetInfo> AllWidgets { get; set; }

        public int TotalDistinctWidgetsCount { get; set; }

        public void PopulateBasicWidgetInformation(string selectedSiteId, Guid? templateId = null, Guid? pageId = null, WidgetLocationMode locationMode = WidgetLocationMode.Both, FilterArgs filterArgs = null)
        {
            var pageManager = PageManager.GetManager();
            pageManager.Provider.SuppressSecurityChecks = true;
            var pageControlData = pageManager.GetControls<PageControl>();
            IQueryable<PageControl> filteredPageControlData;
            if (pageId != null)
            {
                filteredPageControlData = pageControlData.Where(p => p.Page.Id == pageId);
            }
            else if (locationMode == WidgetLocationMode.Both)
            {
                filteredPageControlData = pageControlData
                    .Where(p => p.Page.NavigationNode.RootNodeId.ToString() == selectedSiteId && p.Page.Status != ContentLifecycleStatus.Deleted && p.Page.Visible);
            }
            else
            {
                filteredPageControlData = Enumerable.Empty<PageControl>().AsQueryable();
            }

            var templateControlData = pageManager.GetControls<TemplateControl>();
            IQueryable<TemplateControl> filteredTemplateControlData;
            if (templateId != null)
            {
                filteredTemplateControlData = templateControlData.Where(t => t.Page.Id == templateId);
            }
            else if (locationMode == WidgetLocationMode.Both)
            {
                filteredTemplateControlData = templateControlData;
            }
            else
            {
                filteredTemplateControlData = Enumerable.Empty<TemplateControl>().AsQueryable();
            }

            this.PopulateWidgetsInfo(filteredPageControlData, filteredTemplateControlData, filterArgs);
        }

        public static List<PagesInfo> GetPagesWithForm(string id, string renderer, out int pagesCount, FilterArgs filterArgs)
        {
            var pageManager = PageManager.GetManager();
            pageManager.Provider.SuppressSecurityChecks = true;

            var pageControlData = WidgetReport.GetFormLocation<PageControl>(pageManager, renderer, id).Select(cd => cd.Page);

            var distinctPages = pageControlData.Distinct().AsQueryable();
            pagesCount = distinctPages.Count();
            var pagesInfo = distinctPages.SortBy(filterArgs.SortExpression).Skip(filterArgs.Skip).Take(filterArgs.Take).ToList()

                .Select(p => new PagesInfo()
                {
                    Id = p.Id,
                    PageNodeId = p.NavigationNodeId,
                    Title = HttpUtility.HtmlEncode(p.NavigationNode.Title.GetString(CultureInfo.GetCultureInfo(p.Culture ?? string.Empty), true)),
                    Framework = TemplatesReport.GetFrameworkName((p as IRendererCommonData).Renderer, p.Template),
                    WidgetsCount = WidgetReport.GetWidgetsOnPageCount(pageManager, p),
                    Language = p.Culture != null ? p.Culture.ToUpper() : null,
                    IsSplit = p.NavigationNode.LocalizationStrategy == Telerik.Sitefinity.Localization.LocalizationStrategy.Split,
                    Status = Enum.GetName(typeof(ContentLifecycleStatus), p.Status),
                    IsPublished = p.IsPublished(null),
                    PageUrl = PagesReport.GetEditPageUrl(p.NavigationNode, p.Culture),
                    ParentTemplateIsNullOrMigrated = TemplateInfo.GetParentTemplateIsMigrated(p.Template),
                    SiteId = PagesReport.GetSiteId(p.NavigationNode)
                }).ToList();

            return pagesInfo;
        }

        public static List<PagesInfo> GetPagesWithWidget(string key, bool isMvc, out int pagesCount, FilterArgs filterArgs, string selectedSiteId)
        {
            var pageManager = PageManager.GetManager();
            pageManager.Provider.SuppressSecurityChecks = true;
            var pageControlData = pageManager.GetControls<PageControl>();
            IEnumerable<PageData> filteredPageControlData;
            if (isMvc)
            {
                filteredPageControlData = pageControlData
                .Where(cd => cd.Caption == key && (cd.ObjectType.StartsWith("Telerik.Sitefinity.Mvc") || cd.ObjectType.StartsWith("Telerik.Sitefinity.Frontend")))
                .Include(x => x.Page)
                .Where(cd => cd.Page.Status != ContentLifecycleStatus.Deleted && cd.Page.Visible)
                .Where(cd => cd.Page.NavigationNode != null
                && cd.Page.NavigationNode.RootNodeId.ToString() == selectedSiteId && !cd.Page.NavigationNode.IsDeleted).Select(cd => cd.Page).ToList();
            }
            else
            {
                filteredPageControlData = pageControlData
                .Where(cd => cd.ObjectType == key)
                .Include(x => x.Page)
                .Where(cd => cd.Page.Status != ContentLifecycleStatus.Deleted && cd.Page.Visible)
                .Where(cd => cd.Page.NavigationNode != null && cd.Page.NavigationNode.RootNodeId.ToString() == selectedSiteId && !cd.Page.NavigationNode.IsDeleted).Select(cd => cd.Page).ToList();
            }

            string fullSortExpression = null;
            if (!string.IsNullOrWhiteSpace(filterArgs.SortExpression))
            {
                fullSortExpression = filterArgs.SortDirection == "DESC" ? $"{filterArgs.SortExpression} DESC" : filterArgs.SortExpression;
            }

            var groupedPages = filteredPageControlData.GroupBy(p => p.Id).Select(x => x.First()).AsQueryable();
            pagesCount = groupedPages.Count();
            var pagesInfo = groupedPages.SortBy(fullSortExpression).Skip(filterArgs.Skip).Take(filterArgs.Take)
                .Select(p => new PagesInfo()
                {
                    Id = p.Id,
                    PageNodeId = p.NavigationNodeId,
                    Title = HttpUtility.HtmlEncode(p.NavigationNode.Title.GetString(CultureInfo.GetCultureInfo(p.Culture ?? string.Empty), true)),
                    Framework = TemplatesReport.GetFrameworkName((p as IRendererCommonData).Renderer, p.Template),
                    WidgetsCount = WidgetReport.GetWidgetsOnPageCount(pageManager, p),
                    Language = p.Culture != null ? p.Culture.ToUpper() : null,
                    IsSplit = p.NavigationNode.LocalizationStrategy == Telerik.Sitefinity.Localization.LocalizationStrategy.Split,
                    Status = Enum.GetName(typeof(ContentLifecycleStatus), p.Status),
                    IsPublished = p.IsPublished(null),
                    PageUrl = PagesReport.GetEditPageUrl(p.NavigationNode, p.Culture),
                    ParentTemplateIsNullOrMigrated = TemplateInfo.GetParentTemplateIsMigrated(p.Template),
                    SiteId = PagesReport.GetSiteId(p.NavigationNode)
                }).ToList();

            return pagesInfo;
        }

        public static List<TemplateInfo> GetTemplatesWithWidget(string key, bool isMvc, out int templatesCount, FilterArgs filterArgs)
        {
            var pageManager = PageManager.GetManager();
            pageManager.Provider.SuppressSecurityChecks = true;

            var templateControlData = pageManager.GetControls<TemplateControl>();
            IQueryable<PageTemplate> filteredTemplateControlData;
            if (isMvc)
            {
                filteredTemplateControlData = templateControlData.Where(cd => cd.Caption == key && cd.ObjectType != null && (cd.ObjectType.StartsWith("Telerik.Sitefinity.Mvc") || cd.ObjectType.StartsWith("Telerik.Sitefinity.Frontend")))
                .Where(cd => cd.Page != null).Select(cd => cd.Page);
            }
            else
            {
                filteredTemplateControlData = templateControlData.Where(cd => cd.ObjectType == key)
                .Include(x => x.Page)
                .Where(cd => cd.Page.Category != SiteInitializer.BackendRootNodeId)
                .Select(cd => cd.Page);
            }

            var groupedTemplates = filteredTemplateControlData.ToList().GroupBy(p => p.Id).Select(x => x.FirstOrDefault()).AsQueryable();
            templatesCount = groupedTemplates.Count();
            var templatesInfo = TemplatesReport.SortTemplates(filterArgs.SortExpression, filterArgs.SortDirection, groupedTemplates).Skip(filterArgs.Skip).Take(filterArgs.Take)
                .Select(x => new TemplateInfo()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Title = x.Title,
                    Framework = TemplatesReport.GetFrameworkName(x.Renderer, x),
                    ChildTemplatesCount = x.ChildTemplates.Count(),
                    WidgetsCount = WidgetReport.GetWidgetsOnTemplateCount(pageManager, x),
                    UsedOnPages = x.Pages().Where(p => p.NavigationNode.RootNodeId != SiteInitializer.BackendRootNodeId && !p.NavigationNode.IsDeleted).Count(),
                    TemplateUrl = TemplatesReport.GetEditTemplateUrl(x),
                    ParentTemplateIsNullOrMigrated = TemplateInfo.GetParentTemplateIsMigrated(x.ParentTemplate),
                    ParentTemplateName = x.ParentTemplate != null ? x.ParentTemplate.Name : null
                }).ToList();

            return templatesInfo;
        }


        public static List<TemplateInfo> GetTemplatesWithForm(string id, string renderer, out int templatesCount, FilterArgs filterArgs)
        {
            var pageManager = PageManager.GetManager();
            pageManager.Provider.SuppressSecurityChecks = true;

            var templateControlData = WidgetReport.GetFormLocation<TemplateControl>(pageManager, renderer, id).Select(cd => cd.Page);


            var distinctTemplates = templateControlData.Distinct().AsQueryable();
            templatesCount = distinctTemplates.Count();
            var templatesInfo = TemplatesReport.SortTemplates(filterArgs.SortExpression, filterArgs.SortDirection, distinctTemplates).Skip(filterArgs.Skip).Take(filterArgs.Take)
                .Select(x => new TemplateInfo()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Title = x.Title,
                    Framework = TemplatesReport.GetFrameworkName(x.Renderer, x),
                    ChildTemplatesCount = x.ChildTemplates.Count(),
                    WidgetsCount = WidgetReport.GetWidgetsOnTemplateCount(pageManager, x),
                    UsedOnPages = x.Pages().Where(p => p.NavigationNode.RootNodeId != SiteInitializer.BackendRootNodeId && !p.NavigationNode.IsDeleted).Count(),
                    TemplateUrl = TemplatesReport.GetEditTemplateUrl(x),
                    ParentTemplateIsNullOrMigrated = TemplateInfo.GetParentTemplateIsMigrated(x.ParentTemplate),
                    ParentTemplateName = x.ParentTemplate != null ? x.ParentTemplate.Name : null
                }).ToList();

            return templatesInfo;
        }

        public static int GetWidgetsOnTemplateCount(PageManager manager, PageTemplate template)
        {
            var templateControlDataCount = manager.GetControls<TemplateControl>()
                .Where(t => t.Page.Id == template.Id).Count();

            return templateControlDataCount;
        }
        public static IQueryable<T> GetFormLocation<T>(PageManager manager, string framework, string formId, string parentId = null) where T : ControlData
        {
            if (framework == "mvc")
            {
                var controlDataIds = manager.GetProperties().Where(p => p.Name == "FormId" && p.Value == formId && p.ParentProperty != null && p.ParentProperty.Name == "Model")
                    .Select(p => p.ParentProperty.ParentProperty.Control.Id);
                var filteredControlData = parentId == null ? manager.GetControls<T>().Where(c => controlDataIds.Contains(c.Id)) : manager.GetControls<T>().Where(c => controlDataIds.Contains(c.Id) && c.ParentId.ToString() == parentId);

                return filteredControlData;
            }
            else if (framework == "webforms")
            {
                var controlDataIds = manager.GetProperties().Where(p => p.Name == "FormId" && p.Value == formId && p.ParentProperty != null && p.ParentProperty.Name != "Model").Select(p => p.ParentProperty.Control.Id);
                var filteredControlData = parentId == null ? manager.GetControls<T>().Where(c => controlDataIds.Contains(c.Id)) : manager.GetControls<T>().Where(c => controlDataIds.Contains(c.Id) && c.ParentId.ToString() == parentId);

                return filteredControlData;
            }
            else
            {
                var controlData = manager.GetControls<T>();
                var filteredControlData = parentId == null ? controlData : controlData.Where(cd => cd.ParentId.ToString() == parentId);
                var rendererControlData = filteredControlData
                    .Where(t => t.ObjectType == "SitefinityForm");
                var filteredRendererFormControlData = formId == null ? rendererControlData : rendererControlData
                    .Where(t => t.Properties.Any(p => p.Name == "SelectedItems" && p.Value.Contains(formId)));

                return filteredRendererFormControlData;
            }
        }
        public static IEnumerable<string> ExtractFormIds<T>(PageManager manager, string framework, string parentId = null) where T : ControlData
        {
            var controlData = manager.GetControls<T>();
            IQueryable<T> filteredControlData;
            if (typeof(T) == typeof(TemplateControl))
                filteredControlData = parentId == null ? controlData : controlData.Where(cd => (cd as TemplateControl).Page.Id.ToString() == parentId);
            else
                filteredControlData = parentId == null ? controlData : controlData.Where(cd => (cd as PageControl).Page.Id.ToString() == parentId);
            if (framework == "webforms")
            {
                var formsControlData = filteredControlData
                    .Where(t => t.ObjectType == "Telerik.Sitefinity.Modules.Forms.Web.UI.FormsControl").ToList();
                var formIds = formsControlData
                    .Select(t => t.Properties.FirstOrDefault(p => p.Name == "FormId")?.Value);
                return formIds;
            }
            else if (framework == "mvc")
            {
                var mvcFormControlData = filteredControlData
                    .Where(t => t.Caption == "Form" && t.ObjectType != "Telerik.Sitefinity.Modules.Forms.Web.UI.FormsControl").ToList();
                var mvcFormIds = mvcFormControlData
                    .Select(t => t.Properties.FirstOrDefault(p => p.Name == "Settings")?.ChildProperties.FirstOrDefault(p => p.Name == "Model")?.ChildProperties.FirstOrDefault(p => p.Name == "FormId")?.Value);
                return mvcFormIds;
            }
            else
            {
                var rendererControlData = filteredControlData
                    .Where(t => t.ObjectType == "SitefinityForm").ToList();
                var selectedItemsValues = rendererControlData
                    .Select(t => t.Properties.FirstOrDefault(p => p.Name == "SelectedItems").Value);
                var serializer = new JavaScriptSerializer();
                var formIds = selectedItemsValues.Where(item => item != null).Select(item => (string)serializer.Deserialize<dynamic>(item)["ItemIdsOrdered"][0]);
                return formIds;
            }
        }

        public static int GetFormLocationCount<T>(PageManager manager, string formId, FormFramework formFramework, string renderer) where T : ControlData
        {
            var framework = formFramework == FormFramework.WebForms ? "webforms" : renderer == null ? "mvc" : "renderer";
            var formLocations = GetFormLocation<T>(manager, framework, formId);
            var controlDataCount = formLocations == null ? 0 : formLocations.Count();
            return controlDataCount;
        }

        public static int GetCustomWidgetsOnTemplateCount(PageManager manager, PageTemplate template, bool isMvc)
        {
            var templateControlData = manager.GetControls<TemplateControl>()
                .Where(t => t.Page.Id == template.Id).ToList();
            int count = 0;

            if (isMvc)
            {
                count = templateControlData.Where(cd => cd.ObjectType.StartsWith("Telerik.Sitefinity.Mvc"))
                .Where(cd => cd.Properties.Any(p => p.Name == "ControllerName" && !p.Value.StartsWith("Telerik.Sitefinity")))
                .Count();
            }
            else
            {
                var countWebForms = templateControlData
                .Where(cd => !cd.ObjectType.StartsWith("Telerik.Sitefinity"))
                    .Count();
                var countMvc = templateControlData.Where(cd => cd.ObjectType.StartsWith("Telerik.Sitefinity.Mvc"))
                .Where(cd => cd.Properties.Any(p => p.Name == "ControllerName" && !p.Value.StartsWith("Telerik.Sitefinity")))
                .Count();
                count = countWebForms + countMvc;
            }

            return count;
        }

        public static int GetWidgetsOnPageCount(PageManager manager, PageData pageData)
        {
            var controlDataCount = manager.GetControls<PageControl>()
                .Where(t => t.Page.Id == pageData.Id && !t.Page.NavigationNode.IsDeleted && t.Page.NavigationNode.Id != SiteInitializer.BackendRootNodeId).Count();

            return controlDataCount;
        }


        public static int GetCustomWidgetsOnPageCount(PageManager manager, PageData pageData, bool isMvc)
        {
            var pageControlData = manager.GetControls<PageControl>()
                //.Include(x => x.Page)
                .Where(t => t.Page.Id == pageData.Id && ((IRendererCommonData)t.Page).Renderer == null && !t.Page.NavigationNode.IsDeleted && t.Page.NavigationNode.Id != SiteInitializer.BackendRootNodeId).ToList();
            int count = 0;

            if (isMvc)
            {
                count = pageControlData.Where(cd => cd.ObjectType.StartsWith("Telerik.Sitefinity.Mvc"))
                //.Include(x => x.Properties)
                .Where(cd => cd.Properties.Any(p => p.Name == "ControllerName" && !p.Value.StartsWith("Telerik.Sitefinity")))
                .Count();
            }
            else
            {
                var countWebForms = pageControlData.Where(cd => !cd.ObjectType.StartsWith("Telerik.Sitefinity"))
                    .Count();
                var countMvc = pageControlData.Where(cd => cd.ObjectType.StartsWith("Telerik.Sitefinity.Mvc"))
                //.Include(x => x.Properties)
                .Where(cd => cd.Properties.Any(p => p.Name == "ControllerName" && !p.Value.StartsWith("Telerik.Sitefinity")))
                .Count();
                count = countWebForms + countMvc;
            }

            return count;
        }

        private void PopulateWidgetsInfo(IQueryable<PageControl> pageControlData, IQueryable<TemplateControl> templateControlData, FilterArgs filterArgs)
        {
            var nonMvcWidgetsOnTemplates = templateControlData
                .Where(cd => !cd.ObjectType.StartsWith("Telerik.Sitefinity.Mvc") && !cd.ObjectType.StartsWith("Telerik.Sitefinity.Frontend"))
                .Where(cd => cd.Page.Category != DataExtensions.AppSettings.BackendTemplatesCategoryId)
                .Select(cd => new { ObjectType = cd.ObjectType, Renderer = TemplatesReport.GetRenderer(cd.Page) })
                .GroupBy(cd => cd.ObjectType)
                .Select(g => new WidgetInfo(g.Key, g.Key, TemplatesReport.GetFrameworkName(g.First().Renderer, null), 0, g.Count()) { LocationType = "Template" }).ToList();

            var nonMvcWidgetsOnPages = pageControlData
                .Where(cd => !cd.ObjectType.StartsWith("Telerik.Sitefinity.Mvc") && !cd.ObjectType.StartsWith("Telerik.Sitefinity.Frontend"))
                .Where(cd => !cd.Page.NavigationNode.IsDeleted && cd.Page.NavigationNode.RootNodeId != SiteInitializer.BackendRootNodeId)
                .Select(cd => new { ObjectType = cd.ObjectType, Renderer = TemplatesReport.GetRenderer(cd.Page) })
                .GroupBy(t => t.ObjectType)
                .Select(g => new WidgetInfo(g.Key, g.Key, TemplatesReport.GetFrameworkName(g.First().Renderer, null), g.Count(), 0) { LocationType = "Page" }).ToList();

            this.MvcWidgetsOnTemplates = templateControlData
                .Where(cd => cd.ObjectType.StartsWith("Telerik.Sitefinity.Mvc") || cd.ObjectType.StartsWith("Telerik.Sitefinity.Frontend"))
                //.Include(x => x.Properties)
                .Select(cd => new { Caption = cd.Caption, ObjectType = cd.ObjectType, Id = cd.Id })
                .GroupBy(cd => cd.Caption)
                .Select(g => new WidgetInfo(g.Key, g.First().ObjectType, "MVC", 0, g.Count()) { FirstIdInGroup = g.First().Id, LocationType = "Template" }).ToList();

            this.MvcWidgetsOnPages = pageControlData
                .Where(cd => cd.ObjectType.StartsWith("Telerik.Sitefinity.Mvc") || cd.ObjectType.StartsWith("Telerik.Sitefinity.Frontend"))
                .Where(cd => !cd.Page.NavigationNode.IsDeleted && cd.Page.NavigationNode.Id != SiteInitializer.BackendRootNodeId)
                .Select(cd => new { Caption = cd.Caption, ObjectType = cd.ObjectType, Id = cd.Id })
                .GroupBy(cd => cd.Caption)
                .Select(g => new WidgetInfo(g.Key, g.First().ObjectType, "MVC", g.Count(), 0) { FirstIdInGroup = g.First().Id, LocationType = "Page" }).ToList();

            var allMvcWidgets = this.MergeWidgets(this.MvcWidgetsOnTemplates, this.MvcWidgetsOnPages).Where(w => string.IsNullOrEmpty(filterArgs.SearchExpression) || w.Title != null && w.Title.IndexOf(filterArgs.SearchExpression, StringComparison.OrdinalIgnoreCase) >= 0);
            var allNonMvcWidgets = this.MergeWidgets(nonMvcWidgetsOnTemplates, nonMvcWidgetsOnPages).Where(w => string.IsNullOrEmpty(filterArgs.SearchExpression) || w.Title != null && w.Title.IndexOf(filterArgs.SearchExpression, StringComparison.OrdinalIgnoreCase) >= 0);
            this.TotalDistinctWidgetsCount = allMvcWidgets.Count() + allNonMvcWidgets.Count();
            this.AllWidgets = allMvcWidgets.Concat(allNonMvcWidgets).ToList();
            this.AjustRendererWidgetForDynamicWidgets();
            if (filterArgs.SortExpression != null)
            {
                string fullSortExpression = null;
                if (!string.IsNullOrWhiteSpace(filterArgs.SortExpression))
                {
                    fullSortExpression = filterArgs.SortDirection == "DESC" ? $"{filterArgs.SortExpression} DESC" : filterArgs.SortExpression;
                }

                this.AllWidgets = this.AllWidgets.AsQueryable().SortBy(fullSortExpression)
                    .Skip(filterArgs.Skip).Take(filterArgs.Take).ToList();
            }
            else
            {
                this.AllWidgets = this.AllWidgets.OrderByDescending(w => w.CountOnPages).ThenByDescending(w => w.CountOnTemplates)
                    .Skip(filterArgs.Skip).Take(filterArgs.Take).ToList();
            }
        }

        private void AjustRendererWidgetForDynamicWidgets()
        {
            var manager = PageManager.GetManager();
            foreach (var widget in this.AllWidgets)
            {
                if (widget.Framework == "MVC" && string.IsNullOrEmpty(widget.RendererWidget))
                {
                    var control = manager.GetControl<ControlData>(widget.FirstIdInGroup);
                    if (control != null)
                    {
                        var controllerName = control.Properties.FirstOrDefault(p => p.Name == "ControllerName");
                        if (controllerName?.Value == "Telerik.Sitefinity.Frontend.DynamicContent.Mvc.Controllers.DynamicContentController")
                            widget.RendererWidget = "SitefinityContentList";
                    }
                }
            }
        }

        private IList<WidgetInfo> MergeWidgets(IList<WidgetInfo> onTemplates, IList<WidgetInfo> onPages)
        {
            IList<WidgetInfo> allWidgets = onPages;
            foreach (var widget in onTemplates)
            {
                var existingWidget = allWidgets.FirstOrDefault(w => w.Key == widget.Key);
                if (existingWidget == null)
                {
                    allWidgets.Add(widget);
                }
                else
                {
                    existingWidget.CountOnTemplates = widget.CountOnTemplates;
                }
            }
            return allWidgets;
        }
    }

    public class WidgetInfo : IDataItem
    {
        public WidgetInfo(string key, string objectType, string framework, int usedOnPages, int usedOnTemplates)
        {
            this.Key = key;
            this.Framework = framework;
            this.CountOnPages = usedOnPages;
            this.CountOnTemplates = usedOnTemplates;
            this.ObjectType = objectType;

            if (framework == "MVC")
            {
                this.Title = key;
                this.RendererWidget = this.GetRenderControl(key, objectType);
            }
            else if (framework == "Web Forms")
            {
                this.Title = GetWebFormsWidgetName(key);
                this.RendererWidget = this.GetRenderControl(null, key);
            }
            else //it is renderer widget
            {
                this.Title = key;
                this.RendererWidget = key;
            }
        }

        /// Gets or sets the widget name.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the widget name without namespace information.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the number of widgets on pages.
        /// </summary>
        public int CountOnPages { get; set; }

        /// <summary>
        /// Gets or sets the number of widgets on templates.
        /// </summary>
        public int CountOnTemplates { get; set; }

        /// <summary>
        /// Gets or sets the name of the equivalent widget from the .NET Core renderer.
        /// </summary>
        public string RendererWidget { get; set; }

        public string Framework { get; set; }

        public string LocationType { get; set; }

        public string ObjectType { get; set; }

        internal Guid FirstIdInGroup { get; set; }

        [SkipExport]
        public Guid Id { get; set; }

        [SkipExport]
        public object Transaction { get; set; }

        [SkipExport]
        public object Provider { get; set; }

        [SkipExport]
        public DateTime LastModified { get; set; }

        [SkipExport]
        public string ApplicationName { get; set; }

        public static string GetWebFormsWidgetName(string objectType)
        {
            if (objectType != null)
            {
                var typeNames = objectType.Split(',');
                var widgetName = typeNames[0].SplitOnLast('.').Last();
                return widgetName;
            }
            else
                return objectType;
        }

        private string GetRenderControl(string caption, string objectType)
        {
            if (objectType.Contains("Telerik.Sitefinity.Frontend.GridSystem.GridControl"))
                return "SitefinitySection";
            else if (!string.IsNullOrEmpty(caption))
            {
                this.MvcWidgetsMap.TryGetValue(caption, out string newWidgetName);
                return newWidgetName;
            }
            else
            {
                var newWidgetName = this.WebFormsWidgetsMap.FirstOrDefault(w => objectType.Replace(" ", string.Empty).Contains(w.Key));
                return newWidgetName.Value;
            }
        }


        public Dictionary<string, string> MvcWidgetsMap = new Dictionary<string, string>()
        {
            { "Content block", "SitefinityContentBlock"},
            { "Blog posts", "SitefinityContentList"},
            { "News", "SitefinityContentList"},
            { "Events", "SitefinityContentList"},
            { "List", "SitefinityContentList"},
            { "DynamicContentController", "SitefinityContentList"},
            { "Document link", "SitefinityDocumentList"},
            { "Documents list", "SitefinityDocumentList"},
            { "Image", "SitefinityImage"},
            { "Navigation", "SitefinityNavigation"},
            { "Breadcrumb", "SitefinityBreadcrumb"},
            { "Search box", "SitefinitySeachBox"},
            { "Search results", "SitefinitySearchResults"},
            { "Change password", "SitefinityChangePassword"},
            { "Login form", "SitefinityLoginForm"},
            { "Profile", "SitefinityProfile"},
            { "Registration", "SitefinityRegistration"},
            { "Form", "SitefinityForm"},
            { "Tags", "SitefinityClassification"},
            { "Categories", "SitefinityClassification"},
            { "Native chat", "SitefinityNativeChat"},
            { "Content recommendations", "SitefinityRecommendations"},
            { "Search facets", "SitefinityFacets"},
            { "Telerik.Sitefinity.Web.UI.LayoutControl", "SitefinitySection"}
        };

        public Dictionary<string, string> WebFormsWidgetsMap = new Dictionary<string, string>()
        {
            { "Telerik.Sitefinity.Modules.GenericContent.Web.UI.ContentBlock", "SitefinityContentBlock"},
            { "Telerik.Sitefinity.Modules.Blogs.Web.UI.BlogPostView", "SitefinityContentList"},
            { "Telerik.Sitefinity.Modules.News.Web.UI.NewsView", "SitefinityContentList"},
            { "Telerik.Sitefinity.Modules.Events.Web.UI.EventsView", "SitefinityContentList"},
            { "Telerik.Sitefinity.Modules.Lists.Web.UI.ListView", "SitefinityContentList"},
            { "Telerik.Sitefinity.DynamicModules.Web.UI.Frontend.DynamicContentView", "SitefinityContentList"},
            { "Telerik.Sitefinity.DynamicModules.Web.UI.Frontend.HierarchicalContentView", "SitefinityContentList"},
            { "Telerik.Sitefinity.Modules.Libraries.Web.UI.Documents.DocumentLink", "SitefinityDocumentList"},
            { "Telerik.Sitefinity.Modules.Libraries.Web.UI.Documents.DownloadListView", "SitefinityDocumentList"},
            { "Telerik.Sitefinity.Web.UI.PublicControls.ImageControl", "SitefinityImage"},
            { "Telerik.Sitefinity.Web.UI.NavigationControls.LightNavigationControl", "SitefinityNavigation"},
            { "Telerik.Sitefinity.Web.UI.NavigationControls.NavigationControl", "SitefinityNavigation"},
            { "Telerik.Sitefinity.Web.UI.NavigationControls.Breadcrumb.Breadcrumb", "SitefinityBreadcrumb"},
            { "Telerik.Sitefinity.Services.Search.Web.UI.Public.SearchBox", "SitefinitySeachBox"},
            { "Telerik.Sitefinity.Services.Search.Web.UI.Public.SearchResults", "SitefinitySearchResults"},
            { "Telerik.Sitefinity.Web.UI.PublicControls.LoginWidget", "SitefinityLoginForm"},
            { "Telerik.Sitefinity.Security.Web.UI.UserProfileView", "SitefinityProfile"},
            { "Telerik.Sitefinity.Security.Web.UI.RegistrationForm", "SitefinityRegistration"},
            { "Telerik.Sitefinity.Modules.Forms.Web.UI.FormsControl", "SitefinityForm"},
            { "Telerik.Sitefinity.Web.UI.PublicControls.TaxonomyControl", "SitefinityClassification"},
            { "Telerik.Sitefinity.Web.UI.LayoutControl", "SitefinitySection"}
        };
    }

    public class WidgetDetails
    {
        public string PageTitle { get; set; }

        public string PageUrl { get; set; }

        public string PageCulture { get; set; }

        public bool IsOverriden { get; set; }

        public bool IsEditable { get; set; }

        public bool IsPersonalized { get; set; }
    }

    public class PagesInfo : IDataItem
    {
        public Guid PageNodeId { get; set; }

        public Guid Id { get; set; }

        public string Title { get; set; }

        public string Framework { get; set; }

        public string Status { get; set; }

        public string Language { get; set; }

        public bool IsSplit { get; set; }

        public bool IsPublished { get; set; }

        public int ChildTemplatesCount { get; set; }

        public int WidgetsCount { get; set; }

        public int CustomWidgetsCount { get; set; }

        public string ParentTemplateName { get; set; }

        public bool ParentTemplateIsNullOrMigrated { get; set; }

        public string PageUrl { get; set; }

        public string SiteId { get; set; }

        [SkipExport]
        public object Transaction { get; set; }

        [SkipExport]
        public object Provider { get; set; }

        [SkipExport]
        public DateTime LastModified { get; set; }

        [SkipExport]
        public string ApplicationName { get; set; }
    }

    public class TemplateInfo : IDataItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Framework { get; set; }

        public int UsedOnPages { get; set; }

        public int ChildTemplatesCount { get; set; }

        public int WidgetsCount { get; set; }

        public int CustomWidgetsCount { get; set; }

        public string ParentTemplateName { get; set; }

        public bool ParentTemplateIsNullOrMigrated { get; set; }

        public string TemplateUrl { get; set; }

        [SkipExport]
        public object Transaction { get; set; }

        [SkipExport]
        public object Provider { get; set; }

        [SkipExport]
        public DateTime LastModified { get; set; }

        [SkipExport]
        public string ApplicationName { get; set; }

        public static bool GetParentTemplateIsMigrated(PageTemplate parent)
        {
            return parent == null || parent.Renderer != null;
        }
    }

    public class PagesReport
    {
        public int StandardPagesCount { get; set; }

        public int FilteredPageDataCount { get; set; }

        public int TotalPageDataCount { get; set; }

        public IList<PagesInfo> PagesInfo { get; set; }

        public void PopulatePagesInfo(string selectedSiteId, FilterArgs filterArgs, Guid? templateId = null)
        {
            var rootId = DataExtensions.AppSettings.BackendRootNodeId;

            var pageManager = PageManager.GetManager();
            pageManager.Provider.SuppressSecurityChecks = true;
            var pageNodes = pageManager
                .GetPageNodes()
                .Where(p => p.Id != rootId && p.RootNodeId.ToString() == selectedSiteId && !p.IsDeleted);

            var standardPages = pageNodes.Where(p => p.NodeType == NodeType.Standard);
            this.StandardPagesCount = standardPages.Count();
            string fullSortExpression = null;
            if (!string.IsNullOrWhiteSpace(filterArgs.SortExpression))
            {
                fullSortExpression = filterArgs.SortDirection == "DESC" ? $"{filterArgs.SortExpression} DESC" : filterArgs.SortExpression;
            }

            IQueryable<PageData> filteredPages = pageManager
                .GetPageDataList().Include(p => p.NavigationNode)
                .Where(p => p.NavigationNode.Id != rootId && !p.NavigationNode.IsDeleted && p.NavigationNode.NodeType == NodeType.Standard);

            if (templateId != null)
                //filteredPages = pageManager.GetTemplate(templateId.Value).Pages();
                filteredPages = filteredPages.Where(p => p.Template != null && p.Template.Id == templateId.Value);
            else
                filteredPages = filteredPages.Where(p => p.NavigationNode.RootNodeId.ToString() == selectedSiteId);

            var currentCulture = SystemManager.CurrentContext.Culture.Name;
            var defaultCulture = SystemManager.CurrentContext.CurrentSite.DefaultCulture.Name;
            this.TotalPageDataCount = filteredPages.Count();

            if (templateId == null) // filter by culture only for the main templates grid; when in details we want to display all page translations for the template
            {
                filteredPages = filteredPages.Where(p =>
                    p.Culture == currentCulture || (string.IsNullOrEmpty(p.Culture) && p.NavigationNode.Title != null));
            }

            filteredPages = !string.IsNullOrEmpty(filterArgs.SearchExpression) ? filteredPages.Where(p => p.NavigationNode.Title != null &&
            p.NavigationNode.Title.ToLower().Contains(filterArgs.SearchExpression.ToLower())) : filteredPages;

            this.FilteredPageDataCount = filteredPages.Count();

            IQueryable<PageData> sortedPageData;
            if (filterArgs.SortExpression == "Template.Framework")
            {
                if (filterArgs.SortDirection == "DESC" || filterArgs.SortDirection == null)
                    sortedPageData = filteredPages.OrderByDescending(p => (int)p.Template.Framework);
                else
                    sortedPageData = filteredPages.OrderBy(p => (int)p.Template.Framework);
            }
            else
            {
                sortedPageData = filteredPages.SortBy(fullSortExpression);
            }

            var pageInfos = sortedPageData
                .Skip(filterArgs.Skip).Take(filterArgs.Take).ToList()
                .Select(p => new PagesInfo()
                {
                    Id = p.Id,
                    PageNodeId = p.NavigationNodeId,
                    Title = HttpUtility.HtmlEncode(p.NavigationNode.Title),
                    Framework = TemplatesReport.GetFrameworkName((p as IRendererCommonData).Renderer, p.Template),
                    WidgetsCount = WidgetReport.GetWidgetsOnPageCount(pageManager, p),
                    CustomWidgetsCount = WidgetReport.GetCustomWidgetsOnPageCount(pageManager, p, p.Template?.Framework == PageTemplateFramework.Mvc),
                    Language = p.Culture?.ToUpper(),
                    IsSplit = p.NavigationNode.LocalizationStrategy == Telerik.Sitefinity.Localization.LocalizationStrategy.Split,
                    Status = Enum.GetName(typeof(ContentLifecycleStatus), p.Status),
                    IsPublished = p.IsPublished(null),
                    PageUrl = GetEditPageUrl(p.NavigationNode, p.Culture),
                    ParentTemplateIsNullOrMigrated = TemplateInfo.GetParentTemplateIsMigrated(p.Template),
                    SiteId = PagesReport.GetSiteId(p.NavigationNode)
                });

            this.PagesInfo = (filterArgs.SortExpression != null) ? pageInfos.ToList() : pageInfos.OrderByDescending(x => x.IsPublished).ToList();
        }

        private static string GetTitleInCulture(PageData p)
        {
            return HttpUtility.HtmlEncode(p.NavigationNode.Title);
        }

        public List<PagesInfo> GetPageInfo(Guid pageDataId)
        {
            var pageManager = PageManager.GetManager();
            pageManager.Provider.SuppressSecurityChecks = true;
            var pageHierarchy = new List<PagesInfo>();
            var pageData = pageManager.GetPageData(pageDataId);
            var pageNode = pageData.NavigationNode;
            var pageCulture = pageData.Culture != null ? CultureInfo.GetCultureInfo(pageData.Culture) : null;
            do
            {
                var pageInfo = new PagesInfo()
                {
                    Id = pageData != null ? pageData.Id : Guid.Empty,
                    PageNodeId = pageNode.Id,
                    Title = HttpUtility.HtmlEncode(pageNode.Title.GetString(pageCulture, true)),
                    Framework = pageData != null && pageNode.NodeType == NodeType.Standard ? TemplatesReport.GetFrameworkName((pageData as IRendererCommonData).Renderer, pageData.Template) : pageNode.NodeType.ToString(),
                    WidgetsCount = pageData != null ? WidgetReport.GetWidgetsOnPageCount(pageManager, pageData) : 0,
                    CustomWidgetsCount = pageData != null ? WidgetReport.GetCustomWidgetsOnPageCount(pageManager, pageData, pageData.Template?.Framework == PageTemplateFramework.Mvc) : 0,
                    Language = pageData != null ? pageData.Culture?.ToUpper() : null,
                    IsSplit = pageNode.LocalizationStrategy == Telerik.Sitefinity.Localization.LocalizationStrategy.Split,
                    Status = pageData != null ? Enum.GetName(typeof(ContentLifecycleStatus), pageData.Status) : "Published",
                    IsPublished = pageData != null ? pageData.IsPublished(null) : true,
                    PageUrl = pageData != null ? GetEditPageUrl(pageNode, pageData.Culture) : string.Empty,
                    ParentTemplateIsNullOrMigrated = pageData != null ? TemplateInfo.GetParentTemplateIsMigrated(pageData.Template) : true,
                    SiteId = GetSiteId(pageNode)
                };
                pageHierarchy.Add(pageInfo);
                pageNode = pageNode.Parent;
                if (pageNode == null)
                    break;
                pageData = pageNode.GetPageData(pageCulture);
            }
            while (pageNode != null && (pageData != null || pageNode.NodeType == NodeType.Group) && pageNode.Id != SiteInitializer.CurrentFrontendRootNodeId);

            return pageHierarchy;
        }

        public static string GetSiteId(PageNode page)
        {
            if (page == null)
                return null;

            var multisiteContext = SystemManager.CurrentContext as MultisiteContext;
            var site = multisiteContext.GetSiteBySiteMapRoot(page.RootNodeId);
            return site?.Id.ToString();
        }

        public static string GetEditPageUrl(PageNode page, string cultureName = null)
        {
            if (page == null)
                return string.Empty;

            var pageCulture = cultureName != null ? CultureInfo.GetCultureInfo(cultureName) : null;

            var relativePageUrl = page.GetFullUrl(pageCulture, true, false) + "/Action/Edit/";
            if (cultureName != null)
                relativePageUrl = relativePageUrl + cultureName;

            var multisiteContext = SystemManager.CurrentContext as MultisiteContext;
            var site = multisiteContext.GetSiteBySiteMapRoot(page.RootNodeId);
            if (site == null)
            {
                return $"{relativePageUrl}";
            }
            else
            {
                return $"{relativePageUrl}?sf_site={site.Id}";
            }
        }
    }

    public class TemplatesReport
    {
        public int NetCoreTemplatesCount { get; set; }
        public int NextJsTemplatesCount { get; set; }
        public int MvcTemplatesCount { get; set; }
        public int WebFormsTemplatesCount { get; set; }
        public int HybridTemplatesCount { get; set; }

        public int TotalTemplatesCount { get; set; }

        public IList<TemplateInfo> TemplatesInfo { get; set; }

        public void PopulateTemplatesInfo(Guid? siteMapRootNodeId, string siteSelection, FilterArgs filterArgs)
        {
            var rootId = DataExtensions.AppSettings.BackendRootNodeId;

            var pageManager = PageManager.GetManager();
            pageManager.Provider.SuppressSecurityChecks = true;

            var frontendTemplates = pageManager.GetTemplates().Where(x => !x.Category.Equals(DataExtensions.AppSettings.BackendTemplatesCategoryId));

            IQueryable<PageTemplate> templatesPerSite;

            if (siteMapRootNodeId != Guid.Empty && siteSelection == "current")
            {
                templatesPerSite = GetInSite(frontendTemplates, pageManager, siteMapRootNodeId ?? Guid.Empty);
            }
            else if (siteSelection == "none")
            {
                templatesPerSite = NotShared(frontendTemplates, pageManager);
            }
            else
            {
                templatesPerSite = frontendTemplates;
            }

            this.WebFormsTemplatesCount = templatesPerSite
                .Where(t => t.Framework == PageTemplateFramework.WebForms).Count();
            this.HybridTemplatesCount = templatesPerSite
                .Where(t => t.Framework == PageTemplateFramework.Hybrid).Count();
            this.MvcTemplatesCount = templatesPerSite
                .Where(t => t.Framework == PageTemplateFramework.Mvc && t.Renderer == null).Count();
            this.NetCoreTemplatesCount = templatesPerSite
                .Where(t => t.Renderer == "NetCore").Count();
            this.NextJsTemplatesCount = templatesPerSite
                .Where(t => t.Renderer == "NextJS").Count();

            var templatesToRecalculateFramework = templatesPerSite
                .Where(t => t.Framework == PageTemplateFramework.Mvc && t.Renderer == null).Take(300).ToList();
            var templatesInheritesNextJsCount = templatesToRecalculateFramework.Where(t => GetFrameworkName(t.Renderer, t) == "Next.js").Count();
            this.NextJsTemplatesCount += templatesInheritesNextJsCount;
            this.MvcTemplatesCount -= templatesInheritesNextJsCount;

            var templatesInheritesNetCoreCount = templatesToRecalculateFramework.Where(t => GetFrameworkName(t.Renderer, t) == "ASP.NET Core").Count();
            this.NetCoreTemplatesCount += templatesInheritesNetCoreCount;
            this.MvcTemplatesCount -= templatesInheritesNetCoreCount;

            var defaultCulture = SystemManager.CurrentContext.CurrentSite.DefaultCulture.Name;
            var currentCulture = SystemManager.CurrentContext.Culture.Name;
            templatesPerSite = templatesPerSite.Where(t =>
            t.Culture == currentCulture || t.Title != null); // will display templates with no translations only in the default culture

            var filteredTemplates = !string.IsNullOrEmpty(filterArgs.SearchExpression) ? templatesPerSite.Where(t => t.Title != null && t.Title.ToLower().Contains(filterArgs.SearchExpression.ToLower())) : templatesPerSite;

            this.TotalTemplatesCount = filteredTemplates.Count();

            IQueryable<PageTemplate> sortedTemplates = SortTemplates(filterArgs.SortExpression, filterArgs.SortDirection, filteredTemplates);

            this.TemplatesInfo = sortedTemplates.Skip(filterArgs.Skip).Take(filterArgs.Take).ToList().Select(x => new TemplateInfo()
            {
                Id = x.Id,
                Name = x.Name,
                Title = x.Title,
                Framework = GetFrameworkName(x.Renderer, x),
                ChildTemplatesCount = x.ChildTemplates.Count(),
                WidgetsCount = WidgetReport.GetWidgetsOnTemplateCount(pageManager, x),
                CustomWidgetsCount = WidgetReport.GetCustomWidgetsOnTemplateCount(pageManager, x, x.Framework == PageTemplateFramework.Mvc),
                UsedOnPages = x.Pages().Where(p => p.NavigationNode.RootNodeId != rootId && !p.NavigationNode.IsDeleted).Count(),
                TemplateUrl = GetEditTemplateUrl(x),
                ParentTemplateIsNullOrMigrated = TemplateInfo.GetParentTemplateIsMigrated(x.ParentTemplate),
                ParentTemplateName = x.ParentTemplate != null ? x.ParentTemplate.Name : null
            }).ToList();
        }

        internal static IQueryable<PageTemplate> SortTemplates(string sortExpression, string sortDirection, IQueryable<PageTemplate> templatesPerSite)
        {
            string fullSortExpression = null;
            if (!string.IsNullOrWhiteSpace(sortExpression))
            {
                fullSortExpression = sortDirection == "DESC" ? $"{sortExpression} DESC" : sortExpression;
            }

            IQueryable<PageTemplate> sortedTemplates;
            if (sortExpression == null || sortExpression == "Pages().Count()")
            {
                if (sortDirection == "DESC" || sortDirection == null)
                    sortedTemplates = templatesPerSite.Take(200).OrderByDescending(t => t.Pages().Count());
                else
                    sortedTemplates = templatesPerSite.Take(200).OrderBy(t => t.Pages().Count());
            }
            else if (sortExpression == "Framework")
            {
                if (sortDirection == "DESC" || sortDirection == null)
                    sortedTemplates = templatesPerSite.Take(300).OrderByDescending(t => GetFrameworkName(t.Renderer, t));
                else
                    sortedTemplates = templatesPerSite.Take(300).OrderBy(t => GetFrameworkName(t.Renderer, t));
            }
            else
            {
                sortedTemplates = templatesPerSite.SortBy(fullSortExpression);
            }

            return sortedTemplates;
        }

        internal static IQueryable<PageData> GetPageDataBasedOnTemplate(PageTemplate template, Guid siteRoot = default(Guid))
        {
            var publicNodes = template.Pages()
              .Where(p => (p.NavigationNode.RootNode != null && (p.NavigationNode.NodeType == NodeType.Standard)))
           .Where(p => p.NavigationNode.RootNodeId == siteRoot);


            return publicNodes;
        }

        internal static IQueryable<PageTemplate> GetInSite(IQueryable<PageTemplate> templates, PageManager manager, Guid siteMapRootNodeId)
        {
            var multisiteContext = SystemManager.CurrentContext as MultisiteContext;
            var allSites = multisiteContext.GetSites();
            var site = allSites.First(s => s.SiteMapRootNodeId == siteMapRootNodeId);
            var links = manager.Provider.GetSiteItemLinks().Where(l => l.SiteId == site.Id && l.ItemType == "Telerik.Sitefinity.Pages.Model.PageTemplate");
            HashSet<Guid> templateIds = new HashSet<Guid>(links.Select(l => l.ItemId));

            var templatesPerSite = templates.Where(t => templateIds.Contains(t.Id));

            return templatesPerSite;
        }

        internal static IQueryable<PageTemplate> NotShared(IQueryable<PageTemplate> templates, PageManager manager)
        {
            var sharedlinks = manager.Provider.GetSiteItemLinks().Where(l => l.SiteId != Guid.Empty && l.ItemType == "Telerik.Sitefinity.Pages.Model.PageTemplate");
            HashSet<Guid> templateIds = new HashSet<Guid>(sharedlinks.Select(l => l.ItemId));

            var templatesPerSite = templates.Where(t => !templateIds.Contains(t.Id));

            return templatesPerSite;
        }

        public IList<TemplateInfo> GetTemplateParentHierarchy(Guid templateId)
        {
            var pageManager = PageManager.GetManager();
            pageManager.Provider.SuppressSecurityChecks = true;
            var templateHierarchy = new List<TemplateInfo>();

            if (templateId == Guid.Empty)
                return templateHierarchy;

            var rootId = DataExtensions.AppSettings.BackendRootNodeId;
            var template = pageManager.GetTemplate(templateId);
            while (template != null)
            {
                var templateInfo = new TemplateInfo()
                {
                    Id = template.Id,
                    Name = template.Name,
                    Title = template.Title,
                    Framework = GetFrameworkName(template.Renderer, template),
                    ChildTemplatesCount = template.ChildTemplates.Count(),
                    WidgetsCount = WidgetReport.GetWidgetsOnTemplateCount(pageManager, template),
                    CustomWidgetsCount = WidgetReport.GetCustomWidgetsOnTemplateCount(pageManager, template, template.Framework == PageTemplateFramework.Mvc),
                    UsedOnPages = template.Pages().Where(p => p.NavigationNode.RootNodeId != rootId && !p.NavigationNode.IsDeleted).Count(),
                    TemplateUrl = GetEditTemplateUrl(template),
                    ParentTemplateIsNullOrMigrated = TemplateInfo.GetParentTemplateIsMigrated(template.ParentTemplate),
                    ParentTemplateName = template.ParentTemplate != null ? template.ParentTemplate.Name : null
                };
                templateHierarchy.Add(templateInfo);
                template = template.ParentTemplate;
            }

            return templateHierarchy;
        }

        public static string GetEditTemplateUrl(PageTemplate template)
        {
            var relativeRendererPath = $"/Sitefinity/Template/{template.Id}";
            var url = RouteHelper.ResolveUrl(relativeRendererPath, UrlResolveOptions.Absolute);
            return url;
        }

        public static string GetRenderer(PageTemplate template)
        {
            if (template == null)
                return null;

            string renderer = null;

            while (template != null)
            {
                if (template.Renderer != null)
                {
                    renderer = template.Renderer;
                    break;
                }

                template = template.ParentTemplate;
            }

            return renderer;
        }
        public static string GetRenderer(PageData pageData)
        {
            string renderer = (pageData as IRendererCommonData)?.Renderer;
            if (renderer != null)
                return renderer;

            return GetRenderer(pageData.Template);
        }

        public static string GetFrameworkName(string renderer, PageTemplate template)
        {
            if (renderer != null)
                return RendererNameMap.TryGetValue(renderer, out string rendererName) ? rendererName : renderer;

            if (template == null)
                return "Web Forms";

            var originalTemplateFramework = template.Framework;

            if (renderer == null) //For templates based on another template sometimes the renderer info is null, and we have to get it from the parent
            {
                while (template != null)
                {
                    if (template.Renderer != null)
                    {
                        renderer = template.Renderer;
                        break;
                    }

                    template = template.ParentTemplate;
                }
            }

            if (renderer != null)
            {
                return RendererNameMap.TryGetValue(renderer, out string rendererName) ? rendererName : renderer;
            }

            switch (originalTemplateFramework)
            {
                case PageTemplateFramework.Mvc:
                    return "MVC";
                case PageTemplateFramework.WebForms:
                    return "Web Forms";
                default:
                    return "Hybrid";
            }
        }

        internal static Dictionary<string, string> RendererNameMap = new Dictionary<string, string>()
    {
        { "NetCore", "ASP.NET Core" },
        { "NextJS", "Next.js" }
    };
    }

    public class FormDescriptionInfo : IDataItem
    {
        public FormDescriptionInfo()
        {
        }

        public FormDescriptionInfo(string title, string name, string framework, int usedOnPages, int usedOnTemplates)
        {
            this.Title = title;
            this.Framework = framework;
            this.UsedOnPages = usedOnPages;
            this.UsedOnTemplates = usedOnTemplates;
            this.Name = name;
            var formsManager = FormsManager.GetManager();
            formsManager.Provider.SuppressSecurityChecks = true;
            var form = formsManager.GetFormByName(name);
            this.FieldsCount = form.Controls.Count;
            this.Id = form.Id;
            this.FormUrl = FormsReport.GetEditFormUrl(form);
            this.FormEntriesCount = formsManager.GetFormEntries(form).Count();
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Framework { get; set; }
        public int UsedOnPages { get; set; }
        public int UsedOnTemplates { get; set; }
        public int FieldsCount { get; set; }
        public int FormEntriesCount { get; set; }
        public string FormUrl { get; set; }

        [SkipExport]
        public object Transaction { get; set; }

        [SkipExport]
        public object Provider { get; set; }

        [SkipExport]
        public DateTime LastModified { get; set; }

        [SkipExport]
        public string ApplicationName { get; set; }
    }

    public class FormsReport
    {
        public int NetCoreFormsCount { get; set; }
        public int NextJsFormsCount { get; set; }
        public int MvcFormsCount { get; set; }
        public int WebFormsFormsCount { get; set; }

        public int TotalFormsCount { get; set; }

        public IList<FormDescriptionInfo> FormsInfo { get; set; }

        public static IList<FormDescriptionInfo> GetFormsInfo<T>(string parentId, string framework) where T : ControlData
        {
            var pageManager = PageManager.GetManager();
            var formsManager = FormsManager.GetManager();
            formsManager.Provider.SuppressSecurityChecks = true;
            var formIds = WidgetReport.ExtractFormIds<T>(pageManager, framework.ToLower(), parentId);
            var forms = formsManager.GetForms().Where(f => formIds.Contains(f.Id.ToString())).ToList();

            return forms.Select(x => new FormDescriptionInfo()
            {
                Id = x.Id,
                Name = x.Name,
                Title = x.Title,
                Framework = GetFrameworkName(x.Renderer, x),
                UsedOnTemplates = WidgetReport.GetFormLocationCount<TemplateControl>(pageManager, x.Id.ToString(), x.Framework, x.Renderer),
                FieldsCount = x.Controls.Count,
                FormEntriesCount = x.Status == ContentLifecycleStatus.Live ? formsManager.GetFormEntries(x).Count() : 0,
                UsedOnPages = WidgetReport.GetFormLocationCount<PageControl>(pageManager, x.Id.ToString(), x.Framework, x.Renderer),
                FormUrl = GetEditFormUrl(x)
            }).ToList();
        }

        public void PopulateFormsInfo(Guid? siteMapRootNodeId, string siteSelection, FilterArgs filterArgs)
        {
            var rootId = DataExtensions.AppSettings.BackendRootNodeId;


            var formsManager = FormsManager.GetManager();
            var pageManager = PageManager.GetManager();
            pageManager.Provider.SuppressSecurityChecks = true;
            formsManager.Provider.SuppressSecurityChecks = true;

            var forms = formsManager.GetForms();
            IQueryable<FormDescription> formsPerSite;

            if (siteMapRootNodeId != Guid.Empty && siteSelection == "current")
            {
                formsPerSite = GetInSite(forms, pageManager, siteMapRootNodeId ?? Guid.Empty);
            }
            else if (siteSelection == "none")
            {
                formsPerSite = NotShared(forms, pageManager);
            }
            else
            {
                formsPerSite = forms;
            }

            this.WebFormsFormsCount = formsPerSite
                .Where(f => f.Framework == FormFramework.WebForms).Count();
            this.MvcFormsCount = formsPerSite
                .Where(f => f.Framework == FormFramework.Mvc && f.Renderer == null).Count();
            this.NextJsFormsCount = formsPerSite
                .Where(f => f.Renderer == "NextJs").Count();
            this.NetCoreFormsCount = formsPerSite
                .Where(f => f.Renderer == "NetCore").Count();

            var defaultCulture = SystemManager.CurrentContext.CurrentSite.DefaultCulture;
            var currentCulture = SystemManager.CurrentContext.Culture;
            formsPerSite = formsPerSite.Where(t =>
            t.Title != null); // will display templates with no translations only in the default culture

            var filteredForms = !string.IsNullOrEmpty(filterArgs.SearchExpression) ? formsPerSite.Where(t => t.Title != null && t.Title.ToLower().Contains(filterArgs.SearchExpression.ToLower())) : formsPerSite;

            this.TotalFormsCount = filteredForms.Count();

            IQueryable<FormDescription> sortedForms = SortForms(filterArgs.SortExpression, filterArgs.SortDirection, filteredForms);

            this.FormsInfo = sortedForms.Skip(filterArgs.Skip).Take(filterArgs.Take).ToList().Select(x => new FormDescriptionInfo()
            {
                Id = x.Id,
                Name = x.Name,
                Title = x.Title,
                Framework = GetFrameworkName(x.Renderer, x),
                UsedOnTemplates = WidgetReport.GetFormLocationCount<TemplateControl>(pageManager, x.Id.ToString(), x.Framework, x.Renderer),
                FieldsCount = x.Controls.Count,
                FormEntriesCount = x.Status == ContentLifecycleStatus.Live ? formsManager.GetFormEntries(x).Count() : 0,
                UsedOnPages = WidgetReport.GetFormLocationCount<PageControl>(pageManager, x.Id.ToString(), x.Framework, x.Renderer),
                FormUrl = GetEditFormUrl(x)
            }).ToList();
        }

        internal static IQueryable<FormDescription> SortForms(string sortExpression, string sortDirection, IQueryable<FormDescription> forms)
        {
            string fullSortExpression = null;
            if (!string.IsNullOrWhiteSpace(sortExpression))
            {
                fullSortExpression = sortDirection == "DESC" ? $"{sortExpression} DESC" : sortExpression;
            }

            IQueryable<FormDescription> sortedForms;
            if (sortExpression == null || sortExpression == "Controls.Count")
            {
                sortedForms = forms;
                if (sortDirection == "DESC" || sortDirection == null)
                    sortedForms = forms.OrderByDescending(f => f.Controls.Count);
                else
                    sortedForms = forms.OrderBy(f => f.Controls.Count);
            }
            else if (sortExpression == "Framework")
            {
                if (sortDirection == "DESC" || sortDirection == null)
                    sortedForms = forms.Take(200).OrderByDescending(f => GetFrameworkName(f.Renderer, f));
                else
                    sortedForms = forms.Take(200).OrderBy(f => GetFrameworkName(f.Renderer, f));
            }
            else
            {
                sortedForms = forms.SortBy<FormDescription>(fullSortExpression);
            }

            return sortedForms;
        }

        internal static IQueryable<PageData> GetPageDataBasedOnTemplate(PageTemplate template, Guid siteRoot = default(Guid))
        {
            var publicNodes = template.Pages()
              .Where(p => (p.NavigationNode.RootNode != null && (p.NavigationNode.NodeType == NodeType.Standard)))
           .Where(p => p.NavigationNode.RootNodeId == siteRoot);


            return publicNodes;
        }

        internal static IQueryable<FormDescription> GetInSite(IQueryable<FormDescription> forms, PageManager manager, Guid siteMapRootNodeId)
        {
            var multisiteContext = SystemManager.CurrentContext as MultisiteContext;
            var allSites = multisiteContext.GetSites();
            var site = allSites.First(s => s.SiteMapRootNodeId == siteMapRootNodeId);
            var links = manager.Provider.GetSiteItemLinks().Where(l => l.SiteId == site.Id && l.ItemType == typeof(FormDescription).FullName);
            HashSet<Guid> templateIds = new HashSet<Guid>(links.Select(l => l.ItemId));

            var formsPerSite = forms.Where(t => templateIds.Contains(t.Id));

            return formsPerSite;
        }

        internal static IQueryable<FormDescription> NotShared(IQueryable<FormDescription> forms, PageManager manager)
        {
            var sharedlinks = manager.Provider.GetSiteItemLinks().Where(l => l.SiteId != Guid.Empty && l.ItemType == typeof(FormDescription).FullName);
            HashSet<Guid> templateIds = new HashSet<Guid>(sharedlinks.Select(l => l.ItemId));

            var formsPerSite = forms.Where(t => !templateIds.Contains(t.Id));

            return formsPerSite;
        }

        public static string GetEditFormUrl(FormDescription form)
        {
            if (form.Renderer == null)
            {
                var relativeRendererPath = $"/Sitefinity/adminapp/content/forms/edit/{form.Name}/{SystemManager.CurrentContext.Culture}";
                var url = RouteHelper.ResolveUrl(relativeRendererPath, UrlResolveOptions.Absolute);
                return url;
            }
            else
            {
                var relativeRendererPath = $"/Sitefinity/adminapp/content/forms/sfedit/{form.Id}?sf_culture={SystemManager.CurrentContext.Culture}";
                var url = RouteHelper.ResolveUrl(relativeRendererPath, UrlResolveOptions.Absolute);
                return url;
            }
        }

        public static string GetFrameworkName(string renderer, FormDescription form)
        {
            if (renderer != null)
                return RendererNameMap.TryGetValue(renderer, out string rendererName) ? rendererName : renderer;

            switch (form.Framework)
            {
                case FormFramework.Mvc:
                    return "MVC";
                case FormFramework.WebForms:
                    return "Web Forms";
                default:
                    return "MVC";
            }
        }

        public static object GetFormControls(Guid id)
        {
            var formManager = FormsManager.GetManager();
            var form = formManager.GetForm(id);

            return form.Controls.Select(f => new { Title = GetFieldName(f, "Label") ?? GetFieldName(f, "Title"), Name = GetFieldName(f, "SfFieldName"), FieldType = GetFieldType(f), TextType = GetFieldInputType(f) });
        }

        public static string GetFieldInputType(ControlData controlData)
        {
            var fieldType = GetFieldType(controlData);
            var hasInputType = fieldType == "TextFieldController" || fieldType == "SitefinityTextField";
            if (!hasInputType)
                return null;
            var inputType = GetFieldName(controlData, "InputType");

            return string.IsNullOrEmpty(inputType) ? "Short text" : inputType;
        }

        public static string GetFieldType(ControlData controlData)
        {
            var controllerProperty = controlData.Properties.FirstOrDefault(p => p.Name == "ControllerName" && p.Value != null);
            if (controllerProperty != null)
                return controllerProperty.Value.Split('.').Last();
            else
                return controlData.ObjectType.Split(',').First().Split('.').Last();
        }

        public static string GetFieldName(ControlData control, string propName)
        {
            var fieldNameProp = control.Properties.FirstOrDefault(p => p.Name == propName);
            if (fieldNameProp != null)
                return fieldNameProp.Value;
            else
            {
                var metaField = control.Properties.FirstOrDefault(p => p.Name == "MetaField");
                if (metaField == null || metaField.ChildProperties == null)
                {
                    var settings = control.Properties.FirstOrDefault(p => p.Name == "Settings");
                    if (settings != null && settings.ChildProperties != null)
                    {
                        var model = settings.ChildProperties.FirstOrDefault(p => p.Name == "Model");
                        if (model != null && model.ChildProperties != null)
                        {
                            var modelProperty = model.ChildProperties.FirstOrDefault(p => p.Name == propName);
                            if (modelProperty != null)
                                return modelProperty.Value;
                            metaField = model.ChildProperties.FirstOrDefault(p => p.Name == "MetaField");
                        }
                    }
                }
                return metaField?.ChildProperties?.FirstOrDefault(p => p.Name == (propName == "SfFieldName" ? "FieldName" : propName))?.Value;
            }
        }

        internal static Dictionary<string, string> RendererNameMap = new Dictionary<string, string>()
    {
        { "NetCore", "ASP.NET Core" },
        { "NextJS", "Next.js" }
    };
    }

    public class FilterArgs
    {
        public int Skip { set; get; } = 0;
        public int Take { get; set; } = 1000;
        public string SearchExpression { get; set; }

        public string SortExpression { get; set; }

        public string SortDirection { get; set; }

        public string Culture { get; set; }
    }
}
