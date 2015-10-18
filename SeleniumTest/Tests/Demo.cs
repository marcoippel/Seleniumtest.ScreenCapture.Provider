using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
using OpenQA.Selenium;
using Seleniumtest.Provider.Shared.Enum;
using Seleniumtest.ScreenCapture.Provider;
using SeleniumTest.Business;
using SeleniumTest.Enums;
using SeleniumTest.Helpers;

namespace SeleniumTest.Tests
{
    [TestFixture(Devices.Ipad)]
    [TestFixture(Devices.Nexus5)]
    [TestFixture(Devices.Desktop)]
    public class Demo : SeleniumBase
    {
        private Devices _device;
        private ScreenCaptureProvider _screenCaptureProvider; 
        public Demo()
        {
            this._device = Devices.Desktop;
        }

        public Demo(Devices device)
        {
            this._device = device;
        }

        private const string Url = "/";

        [SetUp]
        public void Initialize()
        {
            SetupDriver(_device);
            _screenCaptureProvider = new ScreenCaptureProvider(TestContext.CurrentContext.TestDirectory);
            _screenCaptureProvider.Start();
        }

        [TearDown]
        public void Cleanup()
        {
            Driver.Quit();
        }

        [Test]
        public void SearchOnGoogle()
        {
            try
            {
                Goto(Url);
                Driver.GetElementByAttribute(ElementType.Input, AttributeType.Class, "gsfi").SendKeys("Selenium tests");
                Driver.GetElementByAttribute(ElementType.Button, AttributeType.Class, "lsb").Click();

                throw new AssertionException("Test");

                const int expectedResult = 5;
                int actualResult = Driver.GetElementsByAttribute(ElementType.Li, AttributeType.Class, "g").Count;

                actualResult.Should().BeGreaterOrEqualTo(expectedResult);
            }
            catch (System.Exception all)
            {
                TakeScreenshot(Driver, all.ToString(), EventType.Error);
                throw;
            }
        }

        private void TakeScreenshot(IWebDriver driver, string message, EventType eventType)
        {
            var methodName = new StackFrame(1, true).GetMethod().Name;
            _screenCaptureProvider.Save(driver.PageSource, driver.Url, message, methodName, eventType);
        }
    }
}
