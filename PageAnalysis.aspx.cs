using ServiceStack;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using Telerik.OpenAccess;
using Telerik.Sitefinity.Abstractions;
using Telerik.Sitefinity.Data.Linq.Dynamic;
using Telerik.Sitefinity.Data.Metadata;
using Telerik.Sitefinity.Lifecycle;
using Telerik.Sitefinity.Model;
using Telerik.Sitefinity.Modules.Pages;
using Telerik.Sitefinity.Multisite;
using Telerik.Sitefinity.Pages.Model;
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
            this.runTemplateAnalysis.Click += RunTemplateAnalysis_Click;
            this.runPageAnalysis.Click += RunPageAnalysis_Click;
            this.GetWidgetsInfo.Click += GetWidgetsInfo_Click;

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
                    this.pageList.Columns[this.pageList.Columns.Count - 1].Visible = false;
                    this.templateList.Columns[this.templateList.Columns.Count - 1].Visible = false;
                    this.templateHierarchyList.Columns[this.templateHierarchyList.Columns.Count - 1].Visible = false;
                    this.singlePageList.Columns[this.singlePageList.Columns.Count - 1].Visible = false;
                }
            }
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

            this.refreshGrids();
        }

        private void ShowHideGrids(string screenName, bool isDetails)
        {
            var allGrids = new List<GridView>() { this.pageList, this.singlePageList, this.templateList, this.templateHierarchyList, this.widgetDetailsGrid, this.AllWidgets, this.singleWidgetList };
            allGrids.ForEach(grid => { grid.Visible = false; });

            var allScreenSpecificElements = new List<System.Web.UI.Control>() { this.backButton, this.siteSelection, this.languageSection, this.pagesInfoAlert, this.templatesInfoAlert,
            this.allWidgetsGridTitle, this.allTemplatesGridTitle, this.allPagesGridTitle };
            allScreenSpecificElements.ForEach(element => { element.Visible = false; });

            this.backButton.Attributes.Add("sf-screenViewName", screenName);
            this.txtSearch.Attributes["placeholder"] = screenName == "Widgets" ? "Search by name" : "Search by title";
            //this.btnSearch.Text = "Search";
            if (!isDetails)
            {
                this.searchSection.Visible = true;
                this.reportHeader.Text = screenName;
                this.templateList.Attributes["WidgetKey"] = string.Empty;
                this.templateList.Attributes["PageId"] = string.Empty;
                this.templateList.AllowSorting = true;
                this.pageList.Attributes["WidgetKey"] = string.Empty;
                this.pageList.Attributes["TemplateId"] = string.Empty;
                this.pageList.AllowSorting = true;
                this.AllWidgets.Attributes["PageId"] = string.Empty;
                this.AllWidgets.Attributes["TemplateId"] = string.Empty;
                switch (screenName)
                {
                    case "Templates":
                        this.templateList.Visible = true;
                        this.siteSelection.Visible = true;
                        this.languageSection.Visible = SystemManager.CurrentContext.SystemCultures.Count() > 1;
                        this.templatesInfoAlert.Visible = true;
                        break;
                    case "Pages":
                        this.pageList.Visible = true;
                        this.pagesInfoAlert.Visible = true;
                        this.languageSection.Visible = SystemManager.CurrentContext.SystemCultures.Count() > 1;
                        break;
                    case "Widgets":
                        this.AllWidgets.Visible = true;
                        break;
                }
            }
            else
            {
                this.searchSection.Visible = false;
                this.backButton.Text = "< Back to all " + screenName.ToLower();
                this.backButton.Visible = true;
                switch (screenName)
                {
                    case "Templates":
                        this.templateHierarchyList.Visible = true;
                        this.pageList.Visible = true;
                        this.AllWidgets.Visible = true;
                        this.allWidgetsGridTitle.Visible = true;
                        this.allPagesGridTitle.Visible = true;
                        break;
                    case "Pages":
                        this.singlePageList.Visible = true;
                        this.templateList.Visible = true;
                        this.templateList.AllowSorting = false;
                        this.AllWidgets.Visible = true;
                        this.allWidgetsGridTitle.Visible = true;
                        this.allTemplatesGridTitle.Visible = true;
                        break;
                    case "Widgets":
                        this.singleWidgetList.Visible = true;
                        this.pageList.Visible = true;
                        this.templateList.Visible = true;
                        this.templateList.AllowSorting = true;
                        this.allPagesGridTitle.Visible = true;
                        this.allTemplatesGridTitle.Visible = true;
                        break;
                }
            }
        }

        private void SetUpTemplatesScreen(Guid? siteMapRootNodeId, string siteSelection)
        {
            var screenName = "Templates";
            this.reportHeader.Text = screenName;

            var templatesReport = new TemplatesReport();
            var filterArgs = new FilterArgs() { Take = this.templateList.PageSize, SortDirection = "DESC", SearchExpression = this.txtSearch.Value };
            templatesReport.PopulateTemplatesInfo(siteMapRootNodeId, siteSelection, filterArgs);

            this.templatesInfoLiteral.Text = $"<b>Templates count</b> ASP.NET Core: {templatesReport.NetCoreTemplatesCount}, Next.js: {templatesReport.NextJsTemplatesCount}, MVC: {templatesReport.MvcTemplatesCount}, " +
                $"Web Forms: {templatesReport.WebFormsTemplatesCount}, " +
            $"Hybrid: {templatesReport.HybridTemplatesCount}";

            this.templateList.VirtualItemCount = templatesReport.TotalTemplatesCount;
            this.templateList.Columns[7].Visible = true;
            this.templateList.PageIndex = 0;
            this.templateList.DataSource = templatesReport.TemplatesInfo;
            this.templateList.DataBind();
        }

        protected void RunPageAnalysis_Click(object sender, EventArgs e)
        {
            var screenName = "Pages";
            var selectedSiteId = Request.Form["SitesDropDown"];
            if (sender != null)
                this.txtSearch.Value = string.Empty;
            this.reportHeader.Text = screenName;
            var pagesReport = new PagesReport();
            var filterArgs = new FilterArgs() { Take = this.pageList.PageSize, SearchExpression = (this.txtSearch.Value) };
            pagesReport.PopulatePagesInfo(selectedSiteId, filterArgs, null);

            this.pagesInfo.Text = $"<b>Pages count:</b> Pages: {pagesReport.StandardPagesCount}, Translations: {pagesReport.TotalPageDataCount}";

            this.pageList.VirtualItemCount = pagesReport.FilteredPageDataCount;
            this.pageList.PageIndex = 0;
            this.pageList.Columns[7].Visible = true;
            this.pageList.DataSource = pagesReport.PagesInfo;
            this.pageList.DataBind();

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
                Take = this.AllWidgets.PageSize,
                SearchExpression = this.txtSearch.Value
            });

            this.AllWidgets.DataSource = widgetReport.AllWidgets;
            this.AllWidgets.PageIndex = 0;
            this.AllWidgets.VirtualItemCount = widgetReport.TotalDistinctWidgetsCount;
            this.AllWidgets.RowCommand += WidgetDetails_RowCommand;
            this.AllWidgets.DataBind();

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
                SortExpression = this.AllWidgets.Attributes["SortExpression"],
                SortDirection = this.AllWidgets.Attributes["SortDirection"]
            };
            widgetReport.PopulateBasicWidgetInformation(selectedSiteId, filterArgs: filterArgs);

            this.AllWidgets.DataSource = widgetReport.AllWidgets;
            this.AllWidgets.PageIndex = e.NewPageIndex;
            this.AllWidgets.DataBind();
        }

        protected void Pages_Sorting(object sender, GridViewSortEventArgs e)
        {
            var selectedSiteId = Request.Form["SitesDropDown"];
            var gridView = sender as GridView;
            var pagesReport = new PagesReport();

            var sortDirection = this.pageList.Attributes["SortDirection"] ?? "ASC";
            if (e.SortExpression == this.pageList.Attributes["SortExpression"])
            {
                sortDirection = (sortDirection == "ASC") ? "DESC" : "ASC";
            }

            var templateIdString = this.pageList.Attributes["TemplateId"];
            Guid? nullableTemplateId = null;
            if (Guid.TryParse(templateIdString, out Guid templateId))
                nullableTemplateId = templateId;
            var widgetKey = this.pageList.Attributes["WidgetKey"];
            var widgetFramework = this.pageList.Attributes["WidgetFramework"];

            if (string.IsNullOrEmpty(widgetKey)) // is in main pages screen or in template details screen
            {
                var filterArgs = new FilterArgs()
                {
                    Take = gridView.PageSize,
                    SortExpression = e.SortExpression,
                    SortDirection = sortDirection,
                    SearchExpression = this.txtSearch.Value
                };
                pagesReport.PopulatePagesInfo(selectedSiteId, filterArgs, nullableTemplateId);
                this.pageList.VirtualItemCount = pagesReport.FilteredPageDataCount;
                this.pageList.DataSource = pagesReport.PagesInfo;
            }
            else if (!string.IsNullOrEmpty(widgetKey)) // is in details widget screen
            {
                var pagesWithWidget = WidgetReport.GetPagesWithWidget(widgetKey, widgetFramework.ToLower() == "mvc", out int pagesCount, 0, this.pageList.PageSize, e.SortExpression, sortDirection);
                this.pageList.VirtualItemCount = pagesCount;
                this.pageList.DataSource = pagesWithWidget;
            }

            this.pageList.Attributes["SortExpression"] = e.SortExpression;
            this.pageList.Attributes["SortDirection"] = sortDirection;
            this.pageList.PageIndex = 0;
            this.pageList.DataBind();

            int iSortedColIdx = 0;
            foreach (DataControlField c in gridView.Columns)
            {
                if (c.SortExpression == e.SortExpression)
                {
                    gridView.HeaderRow.Cells[iSortedColIdx].CssClass = sortDirection.ToLower();
                    break;
                }
                iSortedColIdx++;
            }
        }

        protected void Pages_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            var selectedSiteId = Request.Form["SitesDropDown"];
            var gridView = sender as GridView;
            var pagesReport = new PagesReport();
            var pageSize = gridView.PageSize;
            var skip = e.NewPageIndex * pageSize;
            var sortExpression = this.pageList.Attributes["SortExpression"];
            var sortDirection = this.pageList.Attributes["SortDirection"];
            var templateIdString = this.pageList.Attributes["TemplateId"];
            Guid? nullableTemplateId = null;
            if (Guid.TryParse(templateIdString, out Guid templateId))
                nullableTemplateId = templateId;
            var widgetKey = this.pageList.Attributes["WidgetKey"];
            var widgetFramework = this.pageList.Attributes["WidgetFramework"];

            if (string.IsNullOrEmpty(widgetKey))
            {
                var filterArgs = new FilterArgs() { Skip = skip, Take = pageSize, SortDirection = sortDirection, SortExpression = sortExpression, SearchExpression = this.txtSearch.Value };
                pagesReport.PopulatePagesInfo(selectedSiteId, filterArgs, nullableTemplateId);
                this.pageList.VirtualItemCount = pagesReport.FilteredPageDataCount;
                this.pageList.DataSource = pagesReport.PagesInfo;
            }
            else
            {
                var pagesWithWidget = WidgetReport.GetPagesWithWidget(widgetKey, widgetFramework.ToLower() == "mvc", out int pagesCount, skip, this.pageList.PageSize, sortExpression, sortDirection);
                this.pageList.VirtualItemCount = pagesCount;
                this.pageList.DataSource = pagesWithWidget;
            }

            this.pageList.PageIndex = e.NewPageIndex;
            this.pageList.DataBind();
        }

        protected void Templates_Sorting(object sender, GridViewSortEventArgs e)
        {
            var selectedSiteId = Request.Form["SitesDropDown"];
            var siteSelection = Request.Form["siteSelection"];

            Guid? siteId = string.IsNullOrEmpty(selectedSiteId) ? Guid.Empty : Guid.Parse(selectedSiteId);
            var gridView = sender as GridView;

            var templatesReport = new TemplatesReport();
            var sortDirection = this.templateList.Attributes["SortDirection"] ?? "ASC";
            if (e.SortExpression == this.templateList.Attributes["SortExpression"])
            {
                sortDirection = (sortDirection == "ASC") ? "DESC" : "ASC";
            }

            var widgetKey = this.templateList.Attributes["WidgetKey"];
            var widgetFramework = this.templateList.Attributes["WidgetFramework"];
            if (string.IsNullOrEmpty(widgetKey))
            {
                var filterArgs = new FilterArgs() { SortExpression = e.SortExpression, SortDirection = sortDirection, Take = gridView.PageSize, SearchExpression = this.txtSearch.Value };
                templatesReport.PopulateTemplatesInfo(siteId, siteSelection, filterArgs);
                this.templateList.VirtualItemCount = templatesReport.TotalTemplatesCount;
                this.templateList.DataSource = templatesReport.TemplatesInfo;
            }
            else
            {
                var templatesWithWidget = WidgetReport.GetTemplatesWithWidget(widgetKey, widgetFramework.ToLower() == "mvc", out int pagesCount, 0, gridView.PageSize, e.SortExpression, sortDirection);
                this.templateList.VirtualItemCount = pagesCount;
                this.templateList.DataSource = templatesWithWidget;
            }

            this.templateList.Attributes["SortExpression"] = e.SortExpression;
            this.templateList.Attributes["SortDirection"] = sortDirection;
            this.templateList.PageIndex = 0;
            this.templateList.DataBind();

            int iSortedColIdx = 0;
            foreach (DataControlField c in gridView.Columns)
            {
                if (c.SortExpression == e.SortExpression)
                {
                    gridView.HeaderRow.Cells[iSortedColIdx].CssClass = sortDirection.ToLower();
                    break;
                }
                iSortedColIdx++;
            }
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
            var sortExpression = this.templateList.Attributes["SortExpression"];
            var sortDirection = this.templateList.Attributes["SortDirection"];
            var widgetKey = this.templateList.Attributes["WidgetKey"];
            var widgetFramework = this.templateList.Attributes["WidgetFramework"];
            if (string.IsNullOrEmpty(widgetKey))
            {
                var filterArgs = new FilterArgs() { Skip = skip, Take = pageSize, SortExpression = sortExpression, SortDirection = sortDirection, SearchExpression = this.txtSearch.Value };
                templatesReport.PopulateTemplatesInfo(siteId, siteSelection, filterArgs);
                this.templateList.VirtualItemCount = templatesReport.TotalTemplatesCount;
                this.templateList.DataSource = templatesReport.TemplatesInfo;
            }
            else
            {
                var templatesWithWidget = WidgetReport.GetTemplatesWithWidget(widgetKey, widgetFramework.ToLower() == "mvc", out int pagesCount, skip, pageSize, sortExpression, sortDirection);
                this.templateList.VirtualItemCount = pagesCount;
                this.templateList.DataSource = templatesWithWidget;
            }

            this.templateList.PageIndex = e.NewPageIndex;
            this.templateList.DataBind();
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
            var pageFlterArgs = new FilterArgs() { Take = this.pageList.PageSize };
            pagesReport.PopulatePagesInfo(selectedSiteId, pageFlterArgs, parentTemplateId);
            this.pageList.Attributes["TemplateId"] = arg[0];
            this.pageList.Attributes["WidgetKey"] = string.Empty;
            this.pageList.VirtualItemCount = pagesReport.FilteredPageDataCount;
            this.pageList.PageIndex = 0;
            this.pageList.DataSource = pagesReport.PagesInfo;
            this.pageList.DataBind();

            var templateReport = new TemplatesReport();
            var templateHierarchy = templateReport.GetTemplateParentHierarchy(parentTemplateId);
            this.templateHierarchyList.DataSource = templateHierarchy;
            this.templateHierarchyList.VirtualItemCount = templateHierarchy.Count;
            this.templateHierarchyList.DataBind();

            var widgetReport = new WidgetReport();
            var widgetFilterArgs = new FilterArgs() { Take = this.AllWidgets.PageSize };
            widgetReport.PopulateBasicWidgetInformation(selectedSiteId, parentTemplateId, null, WidgetLocationMode.TemplateOnly, widgetFilterArgs);

            this.AllWidgets.Attributes["TemplateId"] = arg[0];
            this.AllWidgets.Attributes["PageId"] = string.Empty;
            this.AllWidgets.DataSource = widgetReport.AllWidgets;
            this.AllWidgets.VirtualItemCount = widgetReport.TotalDistinctWidgetsCount;
            this.AllWidgets.PageIndex = 0;
            this.AllWidgets.RowCommand += WidgetDetails_RowCommand;
            this.AllWidgets.DataBind();
            this.ShowHideGrids("Templates", true);
        }

        protected void PageHierarchy_Click(object sender, GridViewCommandEventArgs gridArgs)
        {
            if (gridArgs.CommandName != "GetDetails")
                return;

            var selectedSiteId = Request.Form["SitesDropDown"];

            var arg = gridArgs.CommandArgument.ToString().Split(';');
            this.allTemplatesGridTitle.Text = "Templates used in this page";
            this.allWidgetsGridTitle.Text = "Widgets used in this pagе";
            this.reportHeader.Text = $"'{arg[2]}' page info";
            var pageId = Guid.Parse(arg[0]);

            var pageReport = new PagesReport();
            var selectedPageHierarchy = pageReport.GetPageInfo(pageId);
            this.singlePageList.DataSource = selectedPageHierarchy;
            this.singlePageList.VirtualItemCount = 1;
            this.singlePageList.DataBind();

            Guid parentTemplateId;
            var manager = new PageManager();
            var template = manager.GetPageData(pageId).Template;
            parentTemplateId = template != null ? template.Id : Guid.Empty;

            var templateReport = new TemplatesReport();
            var templateHierarchy = templateReport.GetTemplateParentHierarchy(parentTemplateId);

            this.templateList.Attributes["PageId"] = arg[0];
            this.templateList.Attributes["WidgetKey"] = string.Empty;
            this.templateList.DataSource = templateHierarchy;
            this.templateList.PageIndex = 0;
            this.templateList.VirtualItemCount = templateHierarchy.Count;
            this.templateList.DataBind();

            var widgetReport = new WidgetReport();
            var filterArgs = new FilterArgs() { Take = this.AllWidgets.PageSize };
            widgetReport.PopulateBasicWidgetInformation(selectedSiteId, null, pageId, WidgetLocationMode.PageOnly, filterArgs: filterArgs);

            this.AllWidgets.Attributes["PageId"] = arg[0];
            this.AllWidgets.Attributes["TemplateId"] = string.Empty;
            this.AllWidgets.DataSource = widgetReport.AllWidgets;
            this.AllWidgets.VirtualItemCount = widgetReport.TotalDistinctWidgetsCount;
            this.AllWidgets.PageIndex = 0;
            this.AllWidgets.RowCommand += WidgetDetails_RowCommand;
            this.AllWidgets.DataBind();

            this.ShowHideGrids("Pages", true);
        }

        protected void WidgetDetails_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            var arg = e.CommandArgument.ToString().Split(';');
            if (arg.Length < 5)
                return;
            int.TryParse(arg[3], out int countOnPages);
            int.TryParse(arg[4], out int countOnTemplates);

            //var widgetDetails = WidgetReport.GetDetailWidgetsInfo(arg[0], arg[1].ToLower() == "page", arg[2].ToLower() == "mvc");
            var templatesWithWidget = WidgetReport.GetTemplatesWithWidget(arg[0], arg[2].ToLower() == "mvc", out int templateCount, 0, this.templateList.PageSize);
            this.templateList.Attributes["WidgetKey"] = arg[0];
            this.templateList.Attributes["WidgetFramework"] = arg[2];
            this.templateList.Attributes["PageId"] = string.Empty;
            this.templateList.DataSource = templatesWithWidget;
            this.templateList.PageIndex = 0;
            this.templateList.VirtualItemCount = templateCount;
            this.templateList.Columns[7].Visible = false;
            this.templateList.DataBind();
            var pagesWithWidget = WidgetReport.GetPagesWithWidget(arg[0], arg[2].ToLower() == "mvc", out int pagesCount, 0, this.pageList.PageSize);
            this.pageList.Attributes["WidgetKey"] = arg[0];
            this.pageList.Attributes["WidgetFramework"] = arg[2];
            this.pageList.Attributes["TemplateId"] = string.Empty;
            this.pageList.DataSource = pagesWithWidget;
            this.pageList.VirtualItemCount = pagesCount;
            this.pageList.PageIndex = 0;
            this.pageList.Columns[7].Visible = false;
            this.pageList.DataBind();
            var singleWidgetInfo = new WidgetInfo(arg[0], arg[5], arg[2], countOnPages, countOnTemplates);
            this.singleWidgetList.DataSource = new List<WidgetInfo>() { singleWidgetInfo };
            this.singleWidgetList.DataBind();

            this.reportHeader.Text = $"'{singleWidgetInfo.Title}' widget info";
            this.allTemplatesGridTitle.Text = "Templates using this widget";
            this.allPagesGridTitle.Text = "Pages using this widget";
            //this.widgetDetailsGrid.Visible = true;
            //this.widgetDetailsGrid.DataSource = widgetDetails;
            //this.widgetDetailsGrid.DataBind();

            this.ShowHideGrids("Widgets", true);
        }
        protected void SitesDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.refreshGrids();
        }

        protected void LanguageDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            SystemManager.CurrentContext.Culture = CultureInfo.GetCultureInfo((sender as DropDownList).SelectedValue);
            this.refreshGrids();
        }

        protected void refreshGrids()
        {
            var screenName = this.backButton.Attributes["sf-screenViewName"];

            if (screenName == "Templates" || string.IsNullOrEmpty(screenName))
                this.RunTemplateAnalysis_Click(null, null);
            else if (screenName == "Pages")
                this.RunPageAnalysis_Click(null, null);
            else if (screenName == "Widgets")
                this.GetWidgetsInfo_Click(null, null);
        }

        protected void AllWidgets_Sorting(object sender, GridViewSortEventArgs e)
        {
            var selectedSiteId = Request.Form["SitesDropDown"];
            var pageIdString = this.AllWidgets.Attributes["PageId"];
            Guid? pageId = null;
            WidgetLocationMode widgetLocation = WidgetLocationMode.Both;
            if (!string.IsNullOrEmpty(pageIdString))
            {
                pageId = Guid.Parse(pageIdString);
                widgetLocation = WidgetLocationMode.PageOnly;
            }
            var templateIdString = this.AllWidgets.Attributes["TemplateId"];
            Guid? templateId = null;
            if (!string.IsNullOrEmpty(templateIdString))
            {
                templateId = Guid.Parse(templateIdString);
                widgetLocation = WidgetLocationMode.TemplateOnly;
            }

            var sortDirection = this.AllWidgets.Attributes["SortDirection"] ?? "ASC";
            if (e.SortExpression == this.AllWidgets.Attributes["SortExpression"])
            {
                sortDirection = (sortDirection == "ASC") ? "DESC" : "ASC";
            }

            var widgetReport = new WidgetReport();
            var filterArgs = new FilterArgs()
            {
                Take = this.AllWidgets.PageSize,
                SortExpression = e.SortExpression,
                SortDirection = sortDirection,
                SearchExpression = this.txtSearch.Value
            };
            widgetReport.PopulateBasicWidgetInformation(selectedSiteId, templateId, pageId, widgetLocation, filterArgs);

            this.AllWidgets.Attributes["SortExpression"] = e.SortExpression;
            this.AllWidgets.Attributes["SortDirection"] = sortDirection;
            this.AllWidgets.DataSource = widgetReport.AllWidgets;
            this.AllWidgets.PageIndex = 0;
            this.AllWidgets.VirtualItemCount = widgetReport.TotalDistinctWidgetsCount;
            this.AllWidgets.RowCommand += WidgetDetails_RowCommand;
            this.AllWidgets.DataBind();
        }
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

    public static List<WidgetDetails> GetDetailWidgetsInfo(string key, bool isOnPage, bool isMvc)
    {
        var pageManager = PageManager.GetManager();
        pageManager.Provider.SuppressSecurityChecks = true;
        if (isOnPage)
        {
            var pageControlData = pageManager.GetControls<PageControl>();
            IQueryable<PageControl> filteredPageControlData;
            if (isMvc)
            {
                filteredPageControlData = pageControlData
                .Where(cd => cd.Caption == key && (cd.ObjectType.StartsWith("Telerik.Sitefinity.Mvc") || cd.ObjectType.StartsWith("Telerik.Sitefinity.Frontend")))
                .Include(x => x.Page)
                .Where(cd => ((IRendererCommonData)cd.Page).Renderer == null && cd.Page.Status != ContentLifecycleStatus.Deleted && cd.Page.Visible)
                .Where(cd => cd.Page.NavigationNode.RootNodeId != SiteInitializer.BackendRootNodeId);
            }
            else
            {
                filteredPageControlData = pageControlData
                .Where(cd => cd.ObjectType == key)
                .Include(x => x.Page)
                .Where(cd => ((IRendererCommonData)cd.Page).Renderer == null && cd.Page.Status != ContentLifecycleStatus.Deleted && cd.Page.Visible)
                .Where(cd => cd.Page.NavigationNode.RootNodeId != SiteInitializer.BackendRootNodeId);
            }

            var widgetsInfo = filteredPageControlData.Select(cd => new WidgetDetails()
            {
                PageTitle = cd.Page.NavigationNode.Title,
                PageUrl = PagesReport.GetEditPageUrl(cd.Page.NavigationNode, cd.Page.Culture),
                PageCulture = cd.Page.Culture,
                IsOverriden = cd.IsOverridedControl,
                IsEditable = cd.Editable,
                IsPersonalized = cd.IsPersonalized
            }).ToList();

            return widgetsInfo;
        }
        else
        {
            var templateControlData = pageManager.GetControls<TemplateControl>();
            IQueryable<TemplateControl> filteredTemplateControlData;
            if (isMvc)
            {
                filteredTemplateControlData = templateControlData.Where(cd => cd.Caption == key && (cd.ObjectType.StartsWith("Telerik.Sitefinity.Mvc") || cd.ObjectType.StartsWith("Telerik.Sitefinity.Frontend")));
            }
            else
            {
                filteredTemplateControlData = templateControlData.Where(cd => cd.ObjectType == key)
                .Include(x => x.Page)
                .Where(cd => cd.Page.Category != SiteInitializer.BackendRootNodeId);
            }

            var widgetsInfo = filteredTemplateControlData.Select(cd => new WidgetDetails()
            {
                PageTitle = cd.Page.Title,
                PageUrl = TemplatesReport.GetEditTemplateUrl(cd.Page),
                PageCulture = cd.Page.Culture,
                IsOverriden = cd.IsOverridedControl,
                IsEditable = cd.Editable,
                IsPersonalized = cd.IsPersonalized
            }).ToList();

            return widgetsInfo;
        }
    }

    public static List<PagesInfo> GetPagesWithWidget(string key, bool isMvc, out int pagesCount, int skip = 0, int take = 100, string sortExpression = null, string sortDirection = null)
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
            .Where(cd => cd.Page.NavigationNode != null && cd.Page.NavigationNode.RootNodeId != SiteInitializer.BackendRootNodeId).Select(cd => cd.Page).ToList();
        }
        else
        {
            filteredPageControlData = pageControlData
            .Where(cd => cd.ObjectType == key)
            .Include(x => x.Page)
            .Where(cd => cd.Page.Status != ContentLifecycleStatus.Deleted && cd.Page.Visible)
            .Where(cd => cd.Page.NavigationNode != null && cd.Page.NavigationNode.RootNodeId != SiteInitializer.BackendRootNodeId).Select(cd => cd.Page).ToList();
        }

        string fullSortExpression = null;
        if (!string.IsNullOrWhiteSpace(sortExpression))
        {
            fullSortExpression = sortDirection == "DESC" ? $"{sortExpression} DESC" : sortExpression;
        }

        var groupedPages = filteredPageControlData.GroupBy(p => p.Id).Select(x => x.First()).AsQueryable();
        pagesCount = groupedPages.Count();
        var pagesInfo = groupedPages.SortBy(fullSortExpression).Skip(skip).Take(take)
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

    public static List<TemplateInfo> GetTemplatesWithWidget(string key, bool isMvc, out int templatesCount, int skip = 0, int take = 100, string sortExpression = null, string sortDirection = null)
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
        var templatesInfo = TemplatesReport.SortTemplates(sortExpression, sortDirection, groupedTemplates).Skip(skip).Take(take)
            .Select(x => new TemplateInfo()
            {
                Id = x.Id,
                Name = x.Name,
                Title = x.Title,
                Framework = TemplatesReport.GetFrameworkName(x.Renderer, x),
                ChildTemplatesCount = x.ChildTemplates.Count(),
                WidgetsCount = WidgetReport.GetWidgetsOnTemplateCount(pageManager, x),
                UsedOnPages = x.Pages().Where(p => p.NavigationNode.Id != SiteInitializer.BackendRootNodeId && !p.NavigationNode.IsDeleted).Count(),
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
            .Where(t => t.Page.Id == pageData.Id).Count();

        return controlDataCount;
    }

    public static int GetCustomWidgetsOnPageCount(PageManager manager, PageData pageData, bool isMvc)
    {
        var pageControlData = manager.GetControls<PageControl>()
            //.Include(x => x.Page)
            .Where(t => t.Page.Id == pageData.Id && ((IRendererCommonData)t.Page).Renderer == null).ToList();
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
        var rendererWidgersOnTemplates = templateControlData
            .Where(cd => !cd.ObjectType.StartsWith("Telerik.Sitefinity.Mvc") && !cd.ObjectType.StartsWith("Telerik.Sitefinity.Frontend"))
            .Where(cd => cd.Page.Category != SiteInitializer.BackendRootNodeId && cd.Page.Renderer != null)
            .Select(cd => new { ObjectType = cd.ObjectType, Renderer = (cd.Page as IRendererCommonData).Renderer })
            .GroupBy(cd => cd.ObjectType)
            .Select(g => new WidgetInfo(g.Key, g.Key, TemplatesReport.GetFrameworkName(g.First().Renderer, null), 0, g.Count()) { LocationType = "Template" }).ToList();

        var rendererWidgetsOnPages = pageControlData
            .Where(cd => !cd.ObjectType.StartsWith("Telerik.Sitefinity.Mvc") && !cd.ObjectType.StartsWith("Telerik.Sitefinity.Frontend"))
            .Where(cd => (cd.Page as IRendererCommonData).Renderer != null)
            .Select(cd => new { ObjectType = cd.ObjectType, Renderer = (cd.Page as IRendererCommonData).Renderer })
            .GroupBy(t => t.ObjectType)
            .Select(g => new WidgetInfo(g.Key, g.Key, TemplatesReport.GetFrameworkName(g.First().Renderer, null), g.Count(), 0) { LocationType = "Page" }).ToList();

        this.WebFormsWidgetsOnTemplates = templateControlData
            .Where(cd => !cd.ObjectType.StartsWith("Telerik.Sitefinity.Mvc") && !cd.ObjectType.StartsWith("Telerik.Sitefinity.Frontend"))
            .Where(cd => cd.Page.Category != SiteInitializer.BackendRootNodeId && cd.Page.Framework != PageTemplateFramework.Mvc)
            .GroupBy(cd => cd.ObjectType)
            .Select(g => new WidgetInfo(g.Key, g.Key, "Web Forms", 0, g.Count()) { LocationType = "Template" }).ToList();

        this.WebFormsWidgetsOnPages = pageControlData
            .Where(cd => !cd.ObjectType.StartsWith("Telerik.Sitefinity.Mvc") && !cd.ObjectType.StartsWith("Telerik.Sitefinity.Frontend"))
            .Where(cd => cd.Page.Template == null || cd.Page.Template.Framework != PageTemplateFramework.Mvc)
            .GroupBy(t => t.ObjectType)
            .Select(g => new WidgetInfo(g.Key, g.Key, "Web Forms", g.Count(), 0) { LocationType = "Page" }).ToList();

        this.MvcWidgetsOnTemplates = templateControlData
            .Where(cd => cd.ObjectType.StartsWith("Telerik.Sitefinity.Mvc") || cd.ObjectType.StartsWith("Telerik.Sitefinity.Frontend"))
            //.Include(x => x.Properties)
            .Select(cd => new { Caption = cd.Caption, ObjectType = cd.ObjectType, Id = cd.Id })
            .GroupBy(cd => cd.Caption)
            .Select(g => new WidgetInfo(g.Key, g.First().ObjectType, "MVC", 0, g.Count()) { FirstIdInGroup = g.First().Id, LocationType = "Template" }).ToList();

        this.MvcWidgetsOnPages = pageControlData
            .Where(cd => cd.ObjectType.StartsWith("Telerik.Sitefinity.Mvc") || cd.ObjectType.StartsWith("Telerik.Sitefinity.Frontend"))
            .Select(cd => new { Caption = cd.Caption, ObjectType = cd.ObjectType, Id = cd.Id })
            .GroupBy(cd => cd.Caption)
            .Select(g => new WidgetInfo(g.Key, g.First().ObjectType, "MVC", g.Count(), 0) { FirstIdInGroup = g.First().Id, LocationType = "Page" }).ToList();

        var allMvcWidgets = this.MergeWidgets(this.MvcWidgetsOnTemplates, this.MvcWidgetsOnPages).Where(w => string.IsNullOrEmpty(filterArgs.SearchExpression) || w.Title != null && w.Title.IndexOf(filterArgs.SearchExpression, StringComparison.OrdinalIgnoreCase) >= 0);
        var allWebFormsWidgets = this.MergeWidgets(this.WebFormsWidgetsOnTemplates, this.WebFormsWidgetsOnPages).Where(w => string.IsNullOrEmpty(filterArgs.SearchExpression) || w.Title != null && w.Title.IndexOf(filterArgs.SearchExpression, StringComparison.OrdinalIgnoreCase) >= 0);
        var allRendererWidgets = this.MergeWidgets(rendererWidgersOnTemplates, rendererWidgetsOnPages).Where(w => string.IsNullOrEmpty(filterArgs.SearchExpression) || w.Title != null && w.Title.IndexOf(filterArgs.SearchExpression, StringComparison.OrdinalIgnoreCase) >= 0);
        this.TotalDistinctWidgetsCount = allMvcWidgets.Count() + allWebFormsWidgets.Count() + allRendererWidgets.Count();
        if (filterArgs.SortExpression != null)
        {
            string fullSortExpression = null;
            if (!string.IsNullOrWhiteSpace(filterArgs.SortExpression))
            {
                fullSortExpression = filterArgs.SortDirection == "DESC" ? $"{filterArgs.SortExpression} DESC" : filterArgs.SortExpression;
            }

            this.AllWidgets = allWebFormsWidgets.Concat(allMvcWidgets).Concat(allRendererWidgets).AsQueryable().SortBy(fullSortExpression)
                .Skip(filterArgs.Skip).Take(filterArgs.Take).ToList();
        }
        else
        {
            this.AllWidgets = allWebFormsWidgets.Concat(allMvcWidgets).Concat(allRendererWidgets).OrderByDescending(w => w.CountOnPages).ThenByDescending(w => w.CountOnTemplates)
                .Skip(filterArgs.Skip).Take(filterArgs.Take).ToList();
        }
        this.AjustRendererWidgetForDynamicWidgets();
    }

    private void AjustRendererWidgetForDynamicWidgets()
    {
        foreach (var widget in this.AllWidgets)
        {
            if (widget.Framework == "MVC" && string.IsNullOrEmpty(widget.RendererWidget))
            {
                var manager = PageManager.GetManager();
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

public class WidgetInfo
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

public class PagesInfo
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
}

public class TemplateInfo
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
            .Where(p => p.NavigationNode.Id != rootId && !p.NavigationNode.IsDeleted && p.NavigationNode.RootNodeId.ToString() == selectedSiteId && p.NavigationNode.NodeType == NodeType.Standard);

        if (templateId != null)
            //filteredPages = pageManager.GetTemplate(templateId.Value).Pages();
            filteredPages = filteredPages.Where(p => p.Template != null && p.Template.Id == templateId.Value);

        var currentCulture = SystemManager.CurrentContext.Culture.Name;
        var defaultCulture = SystemManager.CurrentContext.CurrentSite.DefaultCulture.Name;
        this.TotalPageDataCount = filteredPages.Count();

        filteredPages = filteredPages.Where(p =>
        p.Culture == currentCulture || (string.IsNullOrEmpty(p.Culture) && p.NavigationNode.Title != null));

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

        var defaultCulture = SystemManager.CurrentContext.CurrentSite.DefaultCulture.Name;
        var currentCulture = SystemManager.CurrentContext.Culture.Name;
        templatesPerSite = templatesPerSite.Where(t =>
        t.Culture == currentCulture || (string.IsNullOrEmpty(t.Culture) && currentCulture == defaultCulture)); // will display templates with no translations only in the default culture

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
            UsedOnPages = x.Pages().Where(p => p.NavigationNode.Id != rootId && !p.NavigationNode.IsDeleted).Count(),
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
                sortedTemplates = templatesPerSite.OrderByDescending(t => (int)t.Framework).ThenBy(t => t.Renderer);
            else
                sortedTemplates = templatesPerSite.OrderBy(t => (int)t.Framework).ThenBy(t => t.Renderer);
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
                UsedOnPages = template.Pages().Count(),
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


    public static string GetFrameworkName(string renderer, PageTemplate template)
    {
        if (renderer != null)
            return RendererNameMap[renderer];

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
            return RendererNameMap[renderer];
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

public class FilterArgs
{
    public int Skip { set; get; } = 0;
    public int Take { get; set; } = 1000;
    public string SearchExpression { get; set; }

    public string SortExpression { get; set; }

    public string SortDirection { get; set; }

    public string Culture { get; set; }
}