Welcome to the Customer Login Statistics Web App

This application provides a complete solution for collecting, managing, analyzing, and visualizing login statistics from multiple customers.
It supports both manual XML uploads and automated REST API submissions, and offers interactive dashboards, Excel exports, and full caching for high performance on large datasets. 

Main Features

    1. Upload Page
    Upload XML files containing login statistics for each customer. Files are automatically validated and stored in separate folders based on customer name.
    Features include:
        Select customer from dropdown or create a new customer name
        Automatic backup of existing files before uploading new ones
        File validation (XML only)
        Safe storage into structured customer directories
    2. Files Page
    View and manage all uploaded XML files grouped by customer.
    Functions include:
        Download raw XML files
        Convert XML into Excel (XLSX) and download
        Immediate file deletion (with confirmation)
    3. Dashboard
    A fully interactive chart-based dashboard visualizing login statistics.
    Features include:
        Time filters (From / To)
        Mode switching: Year or Month view
        Chart type selection: Bar or Line
        Export chart as PNG
        Automatic per-customer coloring
        Shared cache (fast load)
    4. REST API for Automated Upload
    Instead of manual file upload, XML files can be uploaded by automated systems via secure API.
    Features include:
        POST endpoint for XML upload
        API Key authentication
        Automatic storage in customer folder
        Full compatibility with Upload.razor
        Shared cache update after each upload
    5. Home Overview Dashboard
    A visual summary of system activity.
    Widgets include:
        Recent 5 Uploads list

Technical Features

    Advanced XML Parsing Engine
    Fully recursive XML parser that extracts:
        Year, HalfYear, Quarter, Month, Week, Day levels
        User and UserGroup statistics
        Nested LoginStatistics (recursive structure)
    Optimized Excel Export Pipeline
    XML files can be converted into:
        Four structured sheets (Total, Users, Groups, Stats)
        True Number cell types
        Freeze Panes (header row)
        AutoFilter on all columns
        Consistent column widths
    Chart Rendering Technology
    Dashboard charts powered by Chart.js with:
        Dynamic datasets per customer
        Colors per customer
        PNG export with spacing and clean layout

How to Use the Application

    Upload XML files via the Upload page.
    Review, download, or delete in Files.
    Visualize login stats using Dashboard.
    Integrate automated systems via the REST API.
    Check system activity on the Home Page.
    Reset cache when updating or replacing files.
