using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace Bkt177496.Service;

/*
 * Web Driver para el servicio: Reservar cita de Notaría
 * Link: https://www.exteriores.gob.es/Consulados/lahabana/es/ServiciosConsulares/Paginas/index.aspx?scco=Cuba&scd=166&scca=Notar%C3%ADa&scs=Actas+notariales
 * Widget: https://www.citaconsular.es/es/hosteds/widgetdefault/2f21cd9c0d8aa26725bf8930e4691d645/bkt177496
*/

class WebDriverService
{

    private static readonly string toolPath = "D:/COSAS_TRABAJO/used_tools";
    private readonly Random _random;
    private readonly ChromeOptions _driverOptions;
    private readonly ChromeDriverService _driverService;
    private readonly ChromeDriver _chromeDriver;

    // Flags
    private bool _isFirstTime = true;

    public WebDriverService()
    {
        _random = new Random();

        var randomWidth = _random.Next(600, 1024 + 1);
        var randomHeight = _random.Next(600, 768 + 1);

        _driverOptions = new ChromeOptions();
        _driverOptions.BinaryLocation = $"{toolPath}/chrome-win64/chrome.exe";

        _driverOptions.AddArguments(
            $"--window-size={randomWidth},{randomHeight}",
            "--disable-blink-features",
            "--disable-blink-features=AutomationControlled",
            "--disable-infobars",
            "--incognito"
        );

        _driverOptions.AddExcludedArguments(
            "--useAutomationExtension",
            "--enable-automation",
            "--ignore-certificate-errors",
            "--safebrowsing-disable-download-protection",
            "--safebrowsing-disable-auto-update",
            "--disable-client-side-phishing-detection"
        );

        //_driverOptions.AddArguments("--user-data-dir=" + $"{toolPath}/ChromeUserData");
        //_driverOptions.AddArguments("--profile-directory=" + "Trabajo");

        _driverService = ChromeDriverService.CreateDefaultService($"{toolPath}", driverExecutableFileName: "chromedriver");
        _chromeDriver = new ChromeDriver(_driverService, _driverOptions);
        _chromeDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(20);

        // Define JavaScript to remove webdriver detection
        string script =
            "const newProto = navigator.__proto__;" +
            "delete newProto.webdriver;" +
            "navigator.__proto__ = newProto;";

        // Run script
        _chromeDriver.ExecuteCdpCommand("Page.addScriptToEvaluateOnNewDocument", new Dictionary<string, object>() {
            { "source", script }
        });
    }

    public async Task RunInit(CancellationToken cancellationToken)
    {
        String linkPaginaEmbajada = "https://www.exteriores.gob.es/Consulados/lahabana/es/ServiciosConsulares/Paginas/index.aspx?scco=Cuba&scd=166&scca=Familia&scs=Matrimonios";
        String xPathLinkReservarCita = "//*[@id='ctl00_ctl45_g_0960185a_d152_41a8_b88b_fcee6141cd7d']/div/section/div/div/p[53]/a";

        _chromeDriver.Navigate().GoToUrl(linkPaginaEmbajada);

        // Click link
        IWebElement agendarCita = _chromeDriver.FindElement(By.XPath(xPathLinkReservarCita));
        WebDriverWait wait = new WebDriverWait(_chromeDriver, TimeSpan.FromSeconds(_random.Next(3, 10)));
        wait.Until(d => agendarCita.Displayed);
        agendarCita.Click();

        await RunAllProcessAsync(cancellationToken);
    }

    public async Task RunAllProcessAsync(CancellationToken cancellationToken)
    {

        if (_isFirstTime)
        {
            _isFirstTime = false;
        }
        else
        {
            _chromeDriver.Navigate().Refresh();
        }

        try
        {
            await TryClickContinueButton(cancellationToken);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Ocurrio un error: {exception.Message}");
            //_chromeDriver.Close();
            await RunAllProcessAsync(cancellationToken);
        }
    }

    public async Task TryClickContinueButton(CancellationToken cancellationToken)
    {

        string lastTab = _chromeDriver.WindowHandles.Last();
        _chromeDriver.SwitchTo().Window(lastTab);
        int randomWait = _random.Next(2, 5);

        // Boton Continue
        Console.WriteLine("Intentando hacer el click en continue !");
        IWebElement continueButton = _chromeDriver.FindElement(By.XPath("/html/body/div/div/form/button"));
        WebDriverWait waitContinueButton = new WebDriverWait(_chromeDriver, TimeSpan.FromSeconds(randomWait));
        waitContinueButton.Until(d => continueButton.Displayed);
        Thread.Sleep(randomWait);
        continueButton.Click();

        // Fukcing loading
        while(Exists(By.XPath("/html/body/div/div/img"))) {
            Console.WriteLine("Esperando por el loading !");
            await Task.Delay(5 * 1000, cancellationToken);
        }

        // Seleccionar un horario
        Console.WriteLine("Intentando hacer el click en el primer horario !");
        IWebElement someSchedule = _chromeDriver.FindElement(By.XPath(@"//*[@id='idDivSlotColumnContainer-1']/a[1]/div"));
        WebDriverWait waitSomeSchedule = new WebDriverWait(_chromeDriver, TimeSpan.FromSeconds(randomWait));
        waitSomeSchedule.Until(d => someSchedule.Displayed);
        //someSchedule.Click();

        // Send notification to all users
        string message = "Hay <b>horarios disponibles</b> para el servicio de Citas de Notaría: <a>https://www.exteriores.gob.es/Consulados/lahabana/es/ServiciosConsulares/Paginas/index.aspx?scco=Cuba&scd=166&scca=Notar%C3%ADa&scs=Actas+notariales</a>";
        Console.WriteLine(message);
        //await _redisSubscriber.PublishAsync(RedisChannel, message);
        // _dbContext.AppMessages.Add(
        //     new AppMessage { Message = message }
        // );
        // await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
        _chromeDriver.Quit();
        //_chromeDriver.Close();
    }

    public bool Exists(By by)
    {
        return _chromeDriver.FindElements(by).Count != 0 ? true : false;
    }
}