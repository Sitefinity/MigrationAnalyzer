<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PageAnalysis.aspx.cs" Inherits="SitefinityWebApp.PageAnalysis" Debug="true" %>
<%@ Register Assembly="Telerik.Sitefinity" Namespace="Telerik.Sitefinity.Web.UI" TagPrefix="sf" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Sitefinity Migration Analyzer</title>
    <link rel="icon" type="image/x-icon" href="/adminapp/favicon.ico" />
     <script>
         function bootstrapFallBack() {

             // create new link tag 
             const link = document.createElement('link');
             link.href = '/ResourcePackages/Bootstrap4/assets/dist/css/main.min.css';
             link.rel = 'stylesheet';

             // add link tag to head section 
             document.getElementsByTagName('head')[0]
                 .appendChild(link);
         } </script>
    <link rel="stylesheet" href="/ResourcePackages/Bootstrap5/assets/dist/css/main.min.css" crossorigin="anonymous" onerror="bootstrapFallBack()" />
     <style>
        .sf-top-header {
            min-height: 56px;
            box-shadow: 0 3px 5px #eee;
            position: relative;
            z-index: 10;
        }
        .sf-dropdown {
            -webkit-appearance: none;
            -moz-appearance: none;
            background-repeat: no-repeat;
            background-position-x: 90%;
            background-position-y: 6px;
            border-radius: 2px !important;
            padding-right: 2rem !important;
            background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 16 16' width='16' height='24' fill='%23212529'%3e%3cpath fill-rule='evenodd' d='M1.646 4.646a.5.5 0 0 1 .708 0L8 10.293l5.646-5.647a.5.5 0 0 1 .708.708l-6 6a.5.5 0 0 1-.708 0l-6-6a.5.5 0 0 1 0-.708z'/%3e%3c/svg%3e")
        }

        .sf-section {
            display: flex;
            min-height: calc(100dvh - 56px);
        }

        .sf-sidebar {
            width: 220px;
            background-color: #F8F9FA;
            border-right: 1px solid #DEE2E6;
        }

        .sf-sidebar ul {
            list-style: none;
            padding: 0;
            margin: 0;
            position: relative;
            margin-right: -1px;
        }

        .sf-sidebar li {
            height: 40px;
            padding-inline: 10px;
            border: 1px solid transparent;
        }

        .sf-sidebar li:not(.active) {
            color: rgba(var(--bs-link-color-rgb), var(--bs-link-opacity, 1));
        }

        .sf-sidebar li:hover,
        .sf-sidebar li.active {
            border: 1px solid #DEE2E6;
            border-top-left-radius: 6px;
            border-bottom-left-radius: 6px;
        }

        .sf-sidebar li.active:has(+ li:hover),
        .sf-sidebar li:hover:has(+ li.active){
            border-bottom-color: transparent;
        }

        .sf-sidebar li.active {
            background-color: #FFFFFF;
            border-right-color: #fff;
        }

        .sf-sidebar__buton {
            background: none;
            border: none;
            height: 100%;
            width: 100%;
            text-align: left;
            color: inherit;
        }

        .sf-radio-group tbody {
            display: flex;
            gap: 1rem;
            background-color: #F8F9FA;
            border: 1px solid #DEE2E6;
            padding: 1rem;
            border-radius: 6px;
        }

        .table.table-sm {
            border: 0;
        }

        .table.table-sm td {
            border: 0;
            border-bottom: 1px solid #DEE2E6;
        }

        .table.table-sm th {
            vertical-align: bottom;
            white-space: nowrap;
            border: 0;
            border-bottom: 1px solid #212529;
        }

        .table.table-sm th a {
            color: var(--bs-table-color);
        }

        .table.table-sm th a:hover {
            text-decoration: none;
        }

        .table.table-sm td,
        .table.table-sm th {
            padding-inline: 6px;
        }

        .table.table-sm th.asc::after,
        .table.table-sm th.desc::after {
            display: inline-block;
            margin-left: 5px;
            transform: scale(1.5);
        }

        .table.table-sm th.desc::after {
            content: "\2193";
        }

        .table.table-sm th.asc::after {
            content: "\2191";
        }

        .alert:empty,
        h2:empty {
            display: none;
        }

        .sf-pager {
            display: flex;
            margin-top: 1rem;
        }

        .sf-pager td {
            padding: 0 !important;
            border: 0px !important;
        }

        .sf-pager tbody td * {
            border: 1px solid #DEE2E6;
        }

        .sf-pager tbody td:first-of-type * {
            border-top-left-radius: 6px;
            border-bottom-left-radius: 6px;
        }

        .sf-pager tbody td:not(:first-of-type) * {
            border-left: 0;
        }

        .sf-pager tbody td:last-of-type * {
            border-top-right-radius: 6px;
            border-bottom-right-radius: 6px;
        }

        .sf-pager tbody td * {
            padding: 6px 12px;
            display: inline-flex;
        }

        .sf-pager tbody td span {
            color: #6C757D;
        }

        .sf-pager tbody td a:hover {
            background-color: #E9ECEF;
            text-decoration: none;
        }

        .sf-main-header {
            min-height: 60px;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <section class="container-fluid">
                <header class="row px-3 sf-top-header">
                    <div class="col align-items-center d-inline-flex">
                        <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 33.62 35.91"><defs><style>.cls-1{fill:#5ce500;}</style></defs><title>progress</title><g id="Layer_2" data-name="Layer 2"><g id="Description"><path class="cls-1" d="M33.62,28.18A1.38,1.38,0,0,1,33,29.26l-8.71,5V14.88L7.47,5.18l8.71-5a1.39,1.39,0,0,1,1.25,0L33.62,9.49ZM20.54,17,9,10.39a1.39,1.39,0,0,0-1.25,0L0,14.88l12.14,7v14l7.77-4.49a1.39,1.39,0,0,0,.63-1.08ZM0,28.9l8.4,4.85V24Z"/></g></g></svg>
                        <div class="h6 ms-2 mb-0">Sitefinity Migration Analyzer</div>
                    </div>
                    <div class="col d-inline-flex align-items-center justify-content-end">
                        <div class="me-2 d-inline-flex flex-shrink-0">Select site</div>
                        <asp:DropDownList ID="SitesDropDown" CssClass="dropdown form-control w-auto sf-dropdown" runat="server" AutoPostBack ="true"
            DataValueField="SiteMapRootNodeId" DataTextField="Name" OnSelectedIndexChanged="SitesDropDown_SelectedIndexChanged"></asp:DropDownList>
                    </div>
                </header>
        </section>
  
        <section class="sf-section container-fluid ps-0">
            <aside class="sf-sidebar pt-4 ps-3">
                <ul>
                    <li><asp:Button ID="runTemplateAnalysis" runat="server" Text ="Templates" CssClass="sf-sidebar__buton" /></li>
                    <li><asp:Button ID="runPageAnalysis" runat="server" Text ="Pages" CssClass="sf-sidebar__buton" /></li>
                    <li><asp:Button ID="GetWidgetsInfo" runat="server" Text ="Widgets" CssClass="sf-sidebar__buton" /></li>
                </ul>
            </aside>
            <main class="sf-main p-3 ps-5 w-100">
                <asp:LinkButton runat="server" ID="backButton" OnClick="BackButton_Click" CssClass="d-inline-block mb-3" />
                <div class="sf-main-header d-flex align-items-center justify-content-between mb-3">
                    <h1 class="h2"><asp:Literal runat="server" ID="reportHeader"></asp:Literal></h1>
                    <asp:RadioButtonList CssClass="sf-radio-group" ID="siteSelection" runat="server" AutoPostBack="true" OnSelectedIndexChanged="RunTemplateAnalysis_Click">
                        <asp:ListItem Value="all">All sites</asp:ListItem>
                        <asp:ListItem Value="current">This site</asp:ListItem>
                        <asp:ListItem Value="none">Not shared with any site</asp:ListItem>
                    </asp:RadioButtonList>
                </div>
                
                    <div>
                        <asp:GridView ID="templateHierarchyList" runat="server" GridLines="Both" PagerStyle-CssClass="sf-pager" PageSize="10" AllowCustomPaging="true" AllowPaging="false" AllowSorting="false"
                            OnSorting="Templates_Sorting" OnPageIndexChanging="Templates_PageIndexChanging"  OnRowCommand="TemplateHierarchy_Click"
                            ShowHeaderWhenEmpty="True" EmptyDataText="No records found" AutoGenerateColumns="false" CssClass="table table-sm">
                            <Columns>
                                <asp:TemplateField HeaderText="Title" SortExpression="Title" >
                                    <ItemTemplate>
                                        <asp:LinkButton runat="server" ID="lnk" Enabled="true" Text='<%# Eval("Title")%>' 
                                                CommandArgument='<%# Eval("Id") +";template;" + Eval("Title") ?? Eval("Name") %>' CommandName="GetDetails" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField DataField="Name" HeaderText="Template name" 
                                    InsertVisible="False" ReadOnly="True" SortExpression="Name"  />
                                <asp:BoundField DataField="Id" HeaderText="ID" 
                                    InsertVisible="False" ReadOnly="True" SortExpression="Id"  />
                                
                                <asp:BoundField DataField="Framework" HeaderText="Framework" 
                                    InsertVisible="False" ReadOnly="True" SortExpression="Framework"  />
                                <asp:BoundField DataField="UsedOnPages" HeaderText="Used in pages" 
                                    InsertVisible="False" ReadOnly="True" SortExpression="Pages().Count()"  />
                                <asp:BoundField DataField="ChildTemplatesCount" HeaderText="Child templates" 
                                    InsertVisible="False" ReadOnly="True"  />
                                <asp:BoundField DataField="WidgetsCount" HeaderText="All widgets" 
                                    InsertVisible="False" ReadOnly="True"  />
                                <asp:BoundField DataField="CustomWidgetsCount" HeaderText="Custom widgets" 
                                    InsertVisible="False" ReadOnly="True"  />
                                <asp:BoundField DataField="ParentTemplateName" HeaderText="Parent template" 
                                    InsertVisible="False" ReadOnly="True"  />
                                <asp:HyperLinkField HeaderText="View in CMS" Text="View" DataNavigateUrlFields="TemplateUrl" 
                                    DataNavigateUrlFormatString="{0}" Target="_blank" />
                                <asp:TemplateField HeaderText="CLI command" >
                                    <ItemTemplate>
                                        <asp:LinkButton runat = "server" OnClientClick = '<%# string.Format("copyTemplateCommandToClipboard(\"{0}\"); return false;", Eval("Id")) %>' >Copy</asp:LinkButton> 
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>

                    </div>
        <div>
            <asp:GridView ID="singlePageList" runat="server" GridLines="Both" AllowSorting="true" PagerStyle-CssClass="sf-pager" PageSize="15" AllowCustomPaging="true" AllowPaging="true" 
    OnSorting="Pages_Sorting" OnPageIndexChanging="Pages_PageIndexChanging" AutoGenerateColumns="false" OnRowCommand="GetWidgetsInfo_Click" CssClass="table table-sm">
    <Columns>
        <asp:BoundField DataField="Title" HeaderText="Title" 
            InsertVisible="False" ReadOnly="True"  />
        <asp:BoundField DataField="PageNodeId" HeaderText="ID" 
            InsertVisible="False" ReadOnly="True" SortExpression="NavigationNodeId"  />
        <asp:BoundField DataField="Language" HeaderText="Translation" 
            InsertVisible="False" ReadOnly="True" SortExpression="Culture"  />
        <asp:BoundField DataField="Framework" HeaderText="Framework" 
            InsertVisible="False" ReadOnly="True" />
        <asp:BoundField DataField="IsPublished" HeaderText="Published" 
            InsertVisible="False" ReadOnly="True" />
        <asp:BoundField DataField="IsSplit" HeaderText="Split page" 
            InsertVisible="False" ReadOnly="True"  />
        <asp:BoundField DataField="WidgetsCount" HeaderText="All widgets" 
            InsertVisible="False" ReadOnly="True"  />
        <asp:BoundField DataField="CustomWidgetsCount" HeaderText="Custom widgets" 
                InsertVisible="False" ReadOnly="True"  />
        <asp:HyperLinkField HeaderText="View in CMS" Text="View" DataNavigateUrlFields="PageUrl" 
            DataNavigateUrlFormatString="{0}" Target="_blank" />
        <asp:TemplateField HeaderText="CLI command" >
            <ItemTemplate>
                <asp:LinkButton  runat = "server"  OnClientClick = '<%# string.Format("copyPageCommandToClipboard(\"{0}\"); return false;", Eval("PageNodeId")) %>' >Copy</asp:LinkButton> 
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
</asp:GridView>
        </div>
                
                <div>
                    <asp:GridView ID="singleWidgetList" OnRowCommand="WidgetDetails_RowCommand"
                        ShowHeaderWhenEmpty="True" EmptyDataText="No records found" runat="server" GridLines="Both" AutoGenerateColumns="false" CssClass="table table-sm">
                        <Columns>
                            <asp:BoundField DataField="Title" HeaderText="Widget name" 
                                InsertVisible="False" ReadOnly="True"/>
                            <asp:BoundField DataField="RendererWidget" HeaderText="Renderer component name" 
                                InsertVisible="False" ReadOnly="True" SortExpression="RendererWidget" />
                            <asp:BoundField DataField="Framework" HeaderText="Framework" 
                                InsertVisible="False" ReadOnly="True" SortExpression="Framework" />
                            <asp:BoundField DataField="CountOnPages" HeaderText="Used in pages" 
                                InsertVisible="False" ReadOnly="True" SortExpression="CountOnPages"  />
                            <asp:BoundField DataField="CountOnTemplates" HeaderText="Used in templates" 
                                InsertVisible="False" ReadOnly="True" SortExpression="CountOnTemplates"  />
                        </Columns>
                    </asp:GridView>
                </div>
        
        <h2 class="h3 mb-3 mt-5"><asp:Label runat="server" ID="allTemplatesGridTitle"></asp:Label></h2>
        <div class="alert alert-primary mb-4" runat="server" id="templatesInfoAlert"><asp:Literal ID="templatesInfoLiteral" runat="server"></asp:Literal></div>
        
        <div>
            <asp:GridView ID="templateList" runat="server" GridLines="Both" PagerStyle-CssClass="sf-pager" PageSize="10" AllowCustomPaging="true" AllowPaging="true" AllowSorting="true"
                OnSorting="Templates_Sorting" OnPageIndexChanging="Templates_PageIndexChanging"  OnRowCommand="TemplateHierarchy_Click"
                ShowHeaderWhenEmpty="True" EmptyDataText="No records found" AutoGenerateColumns="false" CssClass="table table-sm">
                <Columns>
                    <asp:TemplateField HeaderText="Title" SortExpression="Title" >
                        <ItemTemplate>
                            <asp:LinkButton runat="server" ID="lnk" Enabled="true" Text='<%# Eval("Title")%>' 
                                    CommandArgument='<%# Eval("Id") +";template;" + Eval("Title") ?? Eval("Name")%>' CommandName="GetDetails" />
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="Name" HeaderText="Template name" 
                        InsertVisible="False" ReadOnly="True" SortExpression="Name"  />
                    <asp:BoundField DataField="Id" HeaderText="ID" 
                        InsertVisible="False" ReadOnly="True" SortExpression="Id"  />
                    <asp:BoundField DataField="Framework" HeaderText="Framework" 
                        InsertVisible="False" ReadOnly="True" SortExpression="Framework"  />
                    <asp:BoundField DataField="UsedOnPages" HeaderText="Used in pages" 
                        InsertVisible="False" ReadOnly="True" SortExpression="Pages().Count()"  />
                    <asp:BoundField DataField="ChildTemplatesCount" HeaderText="Child templates" 
                        InsertVisible="False" ReadOnly="True"  />
                    <asp:BoundField DataField="WidgetsCount" HeaderText="All widgets" 
                        InsertVisible="False" ReadOnly="True"  />
                    <asp:BoundField DataField="CustomWidgetsCount" HeaderText="Custom widgets" 
                        InsertVisible="False" ReadOnly="True"  />
                    <asp:BoundField DataField="ParentTemplateName" HeaderText="Parent template" 
                        InsertVisible="False" ReadOnly="True"  />
                    <asp:HyperLinkField HeaderText="View in CMS" Text="View" DataNavigateUrlFields="TemplateUrl" 
                        DataNavigateUrlFormatString="{0}" Target="_blank" />
                    <asp:TemplateField HeaderText="CLI command" >
                        <ItemTemplate>
                            <asp:LinkButton  runat = "server"  OnClientClick = '<%# string.Format("copyTemplateCommandToClipboard(\"{0}\"); return false;", Eval("Id")) %>' >Copy</asp:LinkButton> 
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>

        </div>

                
        <h2 class="h3 mb-3 mt-5"><asp:Label runat="server" ID="allPagesGridTitle"></asp:Label></h2>
        <div class="alert alert-primary mb-4"  runat="server" id="pagesInfoAlert"><asp:Literal ID="pagesInfo" runat="server"></asp:Literal></div>
        <div>

        <asp:GridView ID="pageList" runat="server" GridLines="Both" AllowSorting="true" PagerStyle-CssClass="sf-pager" PageSize="15" AllowCustomPaging="true" AllowPaging="true" 
            OnSorting="Pages_Sorting" OnPageIndexChanging="Pages_PageIndexChanging" 
            ShowHeaderWhenEmpty="True" EmptyDataText="No records found" AutoGenerateColumns="false" OnRowCommand="PageHierarchy_Click" CssClass="table table-sm">
            <Columns>
                <asp:TemplateField HeaderText="Title" SortExpression="NavigationNode.Title" >
                    <ItemTemplate>
                        <asp:LinkButton runat="server" ID="lnk" Enabled="true" Text='<%# Eval("Title")%>' 
                                CommandArgument='<%# Eval("Id") +";page;" + Eval("Title")%>' CommandName="GetDetails" />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="PageNodeId" HeaderText="ID" 
                    InsertVisible="False" ReadOnly="True" SortExpression="NavigationNodeId"  />
            
                
                <asp:BoundField DataField="Language" HeaderText="Translation" 
                    InsertVisible="False" ReadOnly="True" SortExpression="Culture"  />
                <asp:BoundField DataField="Framework" HeaderText="Framework" 
                    InsertVisible="False" ReadOnly="True" SortExpression="Template.Framework" />
                <asp:BoundField DataField="IsPublished" HeaderText="Published" 
                    InsertVisible="False" ReadOnly="True" />
                <asp:BoundField DataField="IsSplit" HeaderText="Split page" 
                    InsertVisible="False" ReadOnly="True"  />
                <asp:BoundField DataField="WidgetsCount" HeaderText="All widgets" 
                    InsertVisible="False" ReadOnly="True"  />
                <asp:BoundField DataField="CustomWidgetsCount" HeaderText="Custom widgets" 
                        InsertVisible="False" ReadOnly="True"  />
                <asp:HyperLinkField HeaderText="View in CMS" Text="View" DataNavigateUrlFields="PageUrl" 
                    DataNavigateUrlFormatString="{0}" Target="_blank" />
                <asp:TemplateField HeaderText="CLI command" >
                    <ItemTemplate>
                        <asp:LinkButton  runat = "server"  OnClientClick = '<%# string.Format("copyPageCommandToClipboard(\"{0}\"); return false;", Eval("PageNodeId")) %>' >Copy</asp:LinkButton> 
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
                </div>

                <h2 class="h3 mb-3 mt-5"><asp:Label runat="server" ID="allWidgetsGridTitle"></asp:Label></h2>
                <div>
                    <asp:GridView ID="AllWidgets" OnRowCommand="WidgetDetails_RowCommand" PagerStyle-CssClass="sf-pager" AllowPaging="true" AllowCustomPaging="true" OnPageIndexChanging="AllWidgets_PageIndexChanging"
                         AllowSorting="true" OnSorting="AllWidgets_Sorting"
                        ShowHeaderWhenEmpty="True" EmptyDataText="No records found" runat="server" GridLines="Both" AutoGenerateColumns="false" CssClass="table table-sm">
                        <Columns>
                            <asp:TemplateField HeaderText="Widget name" SortExpression="Title">
                                <ItemTemplate>
                                    <asp:LinkButton runat="server" ID="lnk" Enabled="true" Text='<%# Eval("Title")%>'
                                            CommandArgument='<%# Eval("Key") + ";" +  Eval("LocationType") + ";" +  Eval("Framework")+ ";" +  Eval("CountOnPages") + ";" +  Eval("CountOnTemplates")  + ";" +  Eval("ObjectType") %>' CommandName="GetDetails" />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="RendererWidget" HeaderText="Renderer component name" 
                                InsertVisible="False" ReadOnly="True" SortExpression="RendererWidget" />
                            <asp:BoundField DataField="Framework" HeaderText="Framework" 
                                InsertVisible="False" ReadOnly="True" SortExpression="Framework" />
                            <asp:BoundField DataField="CountOnPages" HeaderText="Used in pages" 
                                InsertVisible="False" ReadOnly="True" SortExpression="CountOnPages"  />
                            <asp:BoundField DataField="CountOnTemplates" HeaderText="Used in templates" 
                                InsertVisible="False" ReadOnly="True" SortExpression="CountOnTemplates"  />
                        </Columns>
                    </asp:GridView>
                </div>
                
                <div>
                    <h3><asp:Label runat="server" ID="WidgetDetailsHeader"></asp:Label></h3>
                    <asp:GridView ID="widgetDetailsGrid" runat="server" GridLines="Both"
                            ShowHeaderWhenEmpty="True" EmptyDataText="No records found" AutoGenerateColumns="false" CssClass="table table-sm">
                            <Columns
                                ><asp:HyperLinkField HeaderText="Page title" DataTextField ="PageTitle" DataNavigateUrlFields="PageUrl" 
                                    DataNavigateUrlFormatString="{0}" Target="_blank" SortExpression="PageTitle" />
                                <asp:BoundField DataField="PageCulture" HeaderText="Page culture" 
                                    InsertVisible="False" ReadOnly="True" SortExpression="PageCulture"  />
                                <asp:BoundField DataField="IsOverriden" HeaderText="Is overriden" 
                                    InsertVisible="False" ReadOnly="True" SortExpression="IsOverriden" />
                                <asp:BoundField DataField="IsEditable" HeaderText="Is editable" 
                                    InsertVisible="False" ReadOnly="True" SortExpression="IsEditable" />
                                <asp:BoundField DataField="IsPersonalized" HeaderText="Is personalized" 
                                    InsertVisible="False" ReadOnly="True" SortExpression="IsPersonalized" />
                            </Columns>
                        </asp:GridView>
                </div>
            </main>
        </section>

    <sf:ResourceLinks id="resourcesLinks" runat="server">
        <sf:ResourceFile JavaScriptLibrary="JQuery"/>
    </sf:ResourceLinks>
    
    <script src="/ResourcePackages/Bootstrap5/assets/dist/js/popper.min.js" crossorigin="anonymous"></script>
    <script src="/ResourcePackages/Bootstrap5/assets/dist/js/bootstrap.min.js" crossorigin="anonymous"></script>

    </form>
    <script>
        async function copyPageCommandToClipboard(pageId) {
            var text = "sf migrate page " + pageId
            try {
                await navigator.clipboard.writeText(text);
            }
            catch (error) {
                console.error(error.message);
            }
        }
        async function copyTemplateCommandToClipboard(pageId) {
            var text = "sf migrate template " + pageId
            try {
                await navigator.clipboard.writeText(text);
            }
            catch (error) {
                console.error(error.message);
            }
        }

        document.addEventListener("DOMContentLoaded", function (event) {
            var ACTIVE_CLASS = "active";
            var buttons = document.querySelectorAll(".sf-sidebar__buton");
            if (!buttons?.length) {
                return;
            }

            var currActiveBtn = document.querySelector("li.active");
            var currOpenSection = document.querySelector("h1");
            var backTitle = document.getElementById("backButton");
            var currTitle = currOpenSection.innerText;
            if (backTitle?.innerText) {
                var words = backTitle.innerText.split(" ");
                currTitle = words[words.length - 1];
            }
            var label = currTitle ? currTitle : activeButtonTitle;
            var btn = Array.from(buttons).find(x => x.value.toLowerCase() === label.toLowerCase());

            btn.parentElement.classList.add(ACTIVE_CLASS);

            if (currActiveBtn && btn.value !== currActiveBtn.firstElementChild.value) {
                clearActiveButton();
            }

            buttons.forEach((button) => {
                button.addEventListener("click", function (event) {
                    clearActiveButton();

                    event.target.parentElement.classList.add(ACTIVE_CLASS);
                });
            });

            function clearActiveButton() {
                var currActive = document.querySelector(`li.${ACTIVE_CLASS}`);

                if (currActive) {
                    currActive.classList.remove(ACTIVE_CLASS);
                }
            }
        });
    </script>
</body>
</html>
