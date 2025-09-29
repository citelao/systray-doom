This is a test app to validate AOT trimming compatibility.

The Systray library is marked as AOT-compatible, which detects issues in the library code, but this test app method guarantees there are no issues in all of the Systray library's dependencies.

To test:

```
dotnet publish -c Release
```

https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming