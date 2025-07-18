using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Linq;
using NUnit.Framework; // For TestContext

namespace WebTests.Pages
{
    /// <summary>
    /// BasePage class includes common reusable actions and waits for all page classes.
    /// It enhances stability and performance on dynamic JS-based sites like Zara.
    /// </summary>
    public abstract class BasePage
    {
        protected IWebDriver driver;
        protected WebDriverWait wait;
        protected Actions actions;
        protected IJavaScriptExecutor jsExecutor;

        public BasePage(IWebDriver driver)
        {
            this.driver = driver;
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
            actions = new Actions(driver);
            jsExecutor = (IJavaScriptExecutor)driver;
        }

        /// <summary>
        /// Waits for element and clicks it. Uses JS fallback if needed.
        /// </summary>
        protected void Click(By by)
        {
            try
            {
                WaitUntilClickable(by).Click();
            }
            catch (Exception)
            {
                var element = WaitAndFind(by);
                jsExecutor.ExecuteScript("arguments[0].click();", element);
            }
        }

        /// <summary>
        /// Scrolls element into view using JavaScript.
        /// </summary>
        protected void ScrollTo(By by)
        {
            var element = WaitAndFind(by);
            jsExecutor.ExecuteScript("arguments[0].scrollIntoView(true);", element);
        }

        /// <summary>
        /// Clears the input field and types the provided text.
        /// </summary>
        protected void Type(By by, string text)
        {
            var element = WaitAndFind(by);
            element.Clear();
            element.SendKeys(text);
        }

        /// <summary>
        /// Waits until the element is visible in the DOM.
        /// </summary>
        protected IWebElement WaitAndFind(By by)
        {
            return wait.Until(ExpectedConditions.ElementIsVisible(by));
        }


        /// <summary>
        /// Waits until the element is clickable (with optional timeout)
        /// </summary>
        protected IWebElement WaitUntilClickable(By by, int timeoutInSeconds = 10)
        {
            var customWait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));
            return customWait.Until(ExpectedConditions.ElementToBeClickable(by));
        }


        /// <summary>
        /// Returns true if the element is visible.
        /// </summary>
        protected bool IsVisible(By by)
        {
            try
            {
                return WaitAndFind(by).Displayed;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Executes a custom JavaScript command.
        /// </summary>
        protected object ExecuteScript(string script, params object[] args)
        {
            return jsExecutor.ExecuteScript(script, args);
        }

        /// <summary>
        /// Hovers the mouse over the specified element.
        /// </summary>
        protected void Hover(By by)
        {
            var element = WaitAndFind(by);
            actions.MoveToElement(element).Perform();
        }

        /// <summary>
        /// Hovers the mouse over the specified IWebElement.
        /// </summary>
        protected void Hover(IWebElement element)
        {
            actions.MoveToElement(element).Perform();
        }

        /// <summary>
        /// Waits until the element contains the specified text.
        /// </summary>
        protected void WaitUntilTextPresent(By by, string text)
        {
            wait.Until(driver => WaitAndFind(by).Text.Contains(text));
        }

        /// <summary>
        /// Waits until the page is fully loaded (document.readyState = 'complete').
        /// </summary>
        protected void WaitForPageLoad()
        {
            wait.Until(d => jsExecutor.ExecuteScript("return document.readyState").ToString() == "complete");
        }

        /// <summary>
        /// Waits until a specific attribute on an element equals the given value.
        /// </summary>
        protected void WaitUntilAttributeEquals(By by, string attribute, string value)
        {
            wait.Until(driver =>
            {
                var element = driver.FindElement(by);
                return element.GetAttribute(attribute) == value;
            });
        }


        /// <summary>
        /// Waits until the specified element is visible and returns it.
        /// </summary>
        protected IWebElement WaitUntilVisible(By by, int timeoutInSeconds = 10)
        {
            var customWait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));
            return customWait.Until(driver =>
            {
                var el = driver.FindElement(by);
                return el.Displayed ? el : null;
            });
        }

        /// <summary>
        /// Safely scrolls to the element, moves mouse to it, and tries both normal and JS click.
        /// </summary>

        protected void SafeClickWithScrollAndHover(By by)
        {
            Exception lastEx = null;

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var element = WaitUntilVisible(by, 10);
                    jsExecutor.ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
                    Thread.Sleep(300); // scroll sonrası animasyon vs.

                    actions.MoveToElement(element).Perform();
                    WaitUntilClickable(by).Click();

                    TestContext.WriteLine("[INFO] Successfully clicked with mouse actions.");
                    return;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    TestContext.WriteLine($"[WARN] Click attempt {i + 1} failed: {ex.Message}");
                    Thread.Sleep(300);
                }
            }

            try
            {
                var fallbackElement = WaitAndFind(by);
                jsExecutor.ExecuteScript("arguments[0].click();", fallbackElement);
                TestContext.WriteLine("[INFO] Fallback JS click succeeded after retries.");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"[ERROR] JS fallback click failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Accepts the Zara cookie consent popup if it is displayed.
        /// Uses XPath to locate and click the "Accept All Cookies" button.
        /// </summary>
        public void AcceptCookiesIfPresent()
        {
            try
            {
                var cookieButton = driver.FindElement(By.XPath("//button[contains(text(), 'TÜM ÇEREZLERİ KABUL ET')]"));
                if (cookieButton.Displayed)
                {
                    cookieButton.Click();
                    TestContext.WriteLine("[INFO] Cookie banner closed.");
                }
            }
            catch (NoSuchElementException)
            {
                TestContext.WriteLine("[INFO] Cookie banner not found.");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"[WARN] Failed to close cookie banner: {ex.Message}");
            }
        }

        /// <summary>
        /// Scrolls the page to bring the specified element into view.
        /// </summary>
        /// <param name="element">The web element to scroll to.</param>
        protected void ScrollToElement(IWebElement element)
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("arguments[0].scrollIntoView(true);", element);
        }


        /// <summary>
        /// Waits for and finds all elements matching the given selector.
        /// </summary>
        /// <param name="by">The selector to find elements.</param>
        /// <returns>List of IWebElement</returns>
        protected IList<IWebElement> WaitAndFindAll(By by)
        {
            // You may want to use WebDriverWait for better reliability
            return driver.FindElements(by);
        }


        /// <summary>
        /// Scroll to element and click safely using Actions class.
        /// </summary>
        /// <param name="by">Element locator</param>
        public void SafeClickWithScroll(By by)
        {
            var element = WaitUntilVisible(by, 10);
            jsExecutor.ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);

            Actions actions = new Actions(driver);
            actions.MoveToElement(element).Click().Perform();

            TestContext.WriteLine($"[INFO] Safe clicked element: {by.ToString()}");
        }



        /// <summary>
        /// Helper method that waits for an element to be visible within a timeout period.
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <param name="timeout">Maximum wait time</param>
        /// <returns>True if element became visible, false if timeout occurred</returns>
        protected bool WaitUntilVisibleOrTimeout(By selector, TimeSpan timeout)
        {
            try
            {
                var wait = new WebDriverWait(driver, timeout);
                wait.Until(drv =>
                {
                    var element = drv.FindElement(selector);
                    return (element != null && element.Displayed) ? element : null;
                });
                return true;
            }
            catch (WebDriverTimeoutException)
            {
                TestContext.WriteLine($"[WARN] Timeout waiting for element: {selector}");
                return false;
            }
        }



        /* Duplicate ScrollToElementWithOffset removed to resolve compile error */

        protected void SafeClickWithScrollAndHover(By by, int offsetY = 150)
        {
            try
            {
                ScrollToElementWithOffset(by, offsetY);
                Hover(by);
                SafeClick(by);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"[ERROR] SafeClickWithScrollAndHover failed for {by}: {ex.Message}");
            }
        }

        // Duplicate Hover method removed to resolve compile error.






        protected bool IsElementPresent(By by)
        {
            try
            {
                return driver.FindElement(by).Displayed;
            }
            catch
            {
                return false;
            }
        }

        protected void WaitUntilInvisible(By by, int timeoutSeconds = 10)
        {
            var localWait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
            localWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.InvisibilityOfElementLocated(by));
        }

        /// <summary>
        /// Safely clicks an element by waiting until it is clickable.
        /// </summary>
        /// <param name="by">Element locator</param>
        protected void SafeClick(By by)
        {
            try
            {
                WaitUntilClickable(by).Click();
                TestContext.WriteLine($"[INFO] SafeClick succeeded for: {by}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"[WARN] SafeClick failed for {by}: {ex.Message}");
                // Fallback to JS click
                try
                {
                    var element = WaitAndFind(by);
                    jsExecutor.ExecuteScript("arguments[0].click();", element);
                    TestContext.WriteLine($"[INFO] JS fallback click succeeded for: {by}");
                }
                catch (Exception jsEx)
                {
                    TestContext.WriteLine($"[ERROR] JS fallback click failed for {by}: {jsEx.Message}");
                }
            }
        }

        /// <summary>
        /// Scrolls to element with a custom vertical offset (e.g., stop ~150px above it)
        /// </summary>
        protected void ScrollToElementWithOffset(By by, int offset = 150)
        {
            try
            {
                var element = WaitAndFind(by);
                string script = $"window.scrollTo({{ top: arguments[0].getBoundingClientRect().top + window.scrollY - {offset}, behavior: 'smooth' }});";
                jsExecutor.ExecuteScript(script, element);
                TestContext.WriteLine($"[INFO] Scrolled to element with {offset}px offset: {by}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"[WARN] Failed to scroll to element with offset: {ex.Message}");
            }
        }

        /// <summary>
        /// Overload: Scrolls to a given IWebElement with a custom vertical offset.
        /// </summary>
        protected void ScrollToElementWithOffset(IWebElement element, int offset = 150)
        {
            try
            {
                string script = $"window.scrollTo({{ top: arguments[0].getBoundingClientRect().top + window.scrollY - {offset}, behavior: 'smooth' }});";
                jsExecutor.ExecuteScript(script, element);
                TestContext.WriteLine($"[INFO] Scrolled to element with {offset}px offset (IWebElement overload).");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"[WARN] Failed to scroll to element with offset (IWebElement overload): {ex.Message}");
            }
        }

        public void ForceClickWithScrollAndHover(IWebElement element, int scrollOffset = -150, int timeoutSeconds = 15)
        {
            try
            {
                // 1. Overlay varsa bekle
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
                wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions
                    .InvisibilityOfElementLocated(By.CssSelector(".add-to-cart-notification-content")));
            }
            catch
            {
                Console.WriteLine("[WARN] Overlay kontrolü geçilemedi. Devam ediliyor.");
            }

            try
            {
                // 2. Scroll offset ile yukarıda bırak
                var js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, arguments[1]);", element, scrollOffset);

                // 3. Hover
                Actions actions = new Actions(driver);
                actions.MoveToElement(element).Perform();

                // 4. Force click via JavaScript
                js.ExecuteScript("arguments[0].click();", element);

                Console.WriteLine("[INFO] Element force clicked with scroll & hover.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ForceClickWithScrollAndHover failed: {ex.Message}");
                throw;
            }
        }
        public void ForceClickWithScrollAndHover(By by)
        {
            var element = WaitAndFind(by);
            ForceClickWithScrollAndHover(element);
        }

        public void ForceClickWithScrollAndHover(IWebElement element)
        {
            try
            {
                ScrollToElementWithOffset(element, 150); // 150px yukarıdan hizala
                Hover(element);                          // Üzerine gel (hover)
                ClickWithRetriesAndJsFallback(element);  // Normal click + JS fallback
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"[ERROR] ForceClickWithScrollAndHover failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Attempts to click the element up to 3 times, falling back to JS click if needed.
        /// </summary>
        protected void ClickWithRetriesAndJsFallback(IWebElement element)
        {
            Exception lastEx = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    element.Click();
                    TestContext.WriteLine("[INFO] Clicked element successfully.");
                    return;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    TestContext.WriteLine($"[WARN] Click attempt {i + 1} failed: {ex.Message}");
                    System.Threading.Thread.Sleep(200);
                }
            }
            try
            {
                jsExecutor.ExecuteScript("arguments[0].click();", element);
                TestContext.WriteLine("[INFO] JS fallback click succeeded.");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"[ERROR] JS fallback click failed: {ex.Message}");
                throw;
            }
        }





        /// <summary>
        /// Waits until the page is fully loaded (document.readyState === 'complete').
        /// </summary>
        private void WaitUntilPageLoad(int timeoutInSeconds = 15)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));
            wait.Until(d =>
                ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").ToString() == "complete"
            );
        }


        /// <summary>
        /// Checks if an element is present within the specified timeout (in seconds).
        /// </summary>
        private bool IsElementPresent(By by, int timeoutInSeconds)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));
                wait.Until(drv => drv.FindElements(by).Count > 0);
                return true;
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }

        /// <summary>
        /// Scrolls the element into view to ensure it is visible.
        /// </summary>
        private void EnsureElementVisible(By by)
        {
            var element = driver.FindElement(by);
            jsExecutor.ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
        }




    }
}