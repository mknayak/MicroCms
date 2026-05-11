(cd ./src/MicroCMS.WebHost  &&  dotnet publish -c Release -r osx-arm64 --self-contained -o ~/websites/micro_cms/cms_webhost ) &
(cd ./src/MicroCMS.Admin.WebHost  &&  dotnet publish -c Release -r osx-arm64 --self-contained -o ~/websites/micro_cms/cms_admin ) &
(cd ./src/MicroCMS.Delivery.WebHost  &&  dotnet publish -c Release -r osx-arm64 --self-contained -o ~/websites/micro_cms/delivery_webhost ) & 
(cd ./src/MicroCMS.SiteHost  &&  dotnet publish -c Release -r osx-arm64 --self-contained -o ~/websites/micro_cms/site_host ) &