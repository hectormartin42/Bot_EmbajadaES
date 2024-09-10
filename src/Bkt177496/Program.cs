using System.Threading.Tasks;
using Bkt177496.Service;

using CancellationTokenSource cts = new ();

WebDriverService service = new WebDriverService();
await service.RunInit(cts.Token);

//Bkt177496Service.RunAllProcessAsync(cts.Token);
Console.ReadL