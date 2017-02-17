# MBF-CustomeStep-Sample
MBF sample custom step and unit test project.

[MBF](https://github.com/liarjo/MediaBlutlerTest01) support extension points by code using custom steps. More details about custom steps [here](https://github.com/liarjo/MediaBlutlerTest01/blob/master/docs/customStep.md)

This sample cusom step load AMS Asset and select the asset file ending on .json, select or create streaming Locator’s asset and send http get callback with the Asset file URL to a http endpoint.

To run the example you need to set configurtion variables on APP.config file on the test project and runt the unit test.

> MediaAccountName

> AssetId

> MediaAccountKey

