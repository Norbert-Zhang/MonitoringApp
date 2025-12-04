Welcome to the Customer Login Statistics Web App

This application provides a centralized solution for collecting, importing, analyzing, and visualizing login statistics for multiple customers of the software system.
It supports XML uploads, automated API imports, interactive dashboards, and Excel export capabilities. 

Main Features

    1. Upload Page
    Upload XML files containing login statistics for each customer. Files are automatically validated and stored in separate folders based on customer name.
    2. Files Page
    View all uploaded XML files grouped by customer.
    Functions include:
        Download XML files directly
        Convert XML into Excel (XLSX) and download
        Delete XML files directly
    3. Dashboard
    Interactive chart displaying login statistics over time.
    Features include:
        Display login trends by Year or by Month
        Support multiple customers at the same time
        Smooth interactive switching without page reload
        Export chart as PNG image
    4. REST API for Automated Upload
    Instead of manual file upload, XML files can be uploaded by automated systems via secure API.
    Features include:
        POST endpoint for XML upload
        API Token authentication
        Automatic storage in customer folder
        Full compatibility with Upload.razor

Technical Features

    XML Parsing Engine
    Fully recursive XML parser that extracts:
        Year, HalfYear, Quarter, Month, Week, Day
        User and UserGroup statistics
        Nested LoginStatistics (recursive structure)
    Excel Export
    XML files can be converted into:
        Total statistics sheet (Users + UserGroups)
        Hierarchical breakdown sheet
        Formatted according to data structure
    Chart Rendering
    The Dashboard is powered by Chart.js with:
        Dynamic dataset generation
        Colors per customer
        Canvas-safe rendering with automatic destroy()

How to Use the Application

    Go to Upload to upload XML files for each customer.
    Check or delete uploaded files anytime on the Files page.
    Use Dashboard to view login trends.
    Integrate with your systems via the REST API.
