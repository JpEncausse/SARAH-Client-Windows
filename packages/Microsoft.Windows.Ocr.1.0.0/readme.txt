
NuGet has successfully installed the Microsoft OCR Library for Windows Runtime into your project!

Finalizing the installation
================================

IMPORTANT: For C++ projects, select OcrResources\MsOcrRes.orp file go to properties and change value of property "Content" to "Yes".

Some versions of Visual Studio may not update references or intellisense.
To fix this, please close and reopen your project.

Check full documentation at http://msdn.microsoft.com/en-us/library/windows/apps/xaml/windowspreview.media.ocr.aspx.


OCR Resource File (MsOcrRes.orp)
================================

Every language uses some language-specific resources.
After you install the package, only the resources required for OCR of English text are included in target project.
If you want to use a custom group of languages in your app:
-Use the OCR Resources Generator tool to generate a new OCR resources file and
-Replace the resources that were injected into your project when you installed the package.

Make sure OcrResource\MsOcrRes.orp is deployed within your app.

For more info, see http://msdn.microsoft.com/en-us/library/windows/apps/windowspreview.media.ocr.aspx#how_to_generate_OCR_resource_files.


Feedback
===========================

Send us feedback at nugetmsocr@microsoft.com.