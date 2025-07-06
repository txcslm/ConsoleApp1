using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace ChubFollowersConsole
{
    class Program
    {
        // Настройки - увеличиваем задержки для надежности
        private const int BatchSize      = 10;
        private const int BatchPauseMs   = 1000;
        private const int OpenPauseMs    = 200;   // увеличили обратно
        private const int DownloadPauseMs= 500;   // увеличили для надежности
        private const int PageTransitionMs = 500; // увеличили
        private const int PageLoadWaitMs = 1000;  // ждем загрузки страницы персонажа

        static void Main(string[] args)
        {
            Console.WriteLine("=== Chub.ai Followers & Characters Downloader ===");

            // Настройка ChromeDriver с прописанной папкой загрузок
            var downloadPath = Path.Combine(Environment.CurrentDirectory, "temp_downloads");
            Directory.CreateDirectory(downloadPath);

            var options = new ChromeOptions();
            options.AddArgument("--user-data-dir=/Users/txcslm/chub_followers_profile");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--no-sandbox");
            
            // Настраиваем папку загрузок
            options.AddUserProfilePreference("download.default_directory", downloadPath);
            options.AddUserProfilePreference("download.prompt_for_download", false);
            options.AddUserProfilePreference("download.directory_upgrade", true);
            options.AddUserProfilePreference("safebrowsing.enabled", false);
            
            // НЕ отключаем картинки, чтобы страница нормально загрузилась
            // options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);
            options.PageLoadStrategy = PageLoadStrategy.Normal; // Ждем полной загрузки

            using var driver = new ChromeDriver(options);
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(3)); // увеличили таймаут
            
            // Ускоряем таймауты
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(3);

            // Корневая папка внутри bin/Debug/net9.0
            var root = Path.Combine(Environment.CurrentDirectory, "followers");
            Directory.CreateDirectory(root);

            // Открываем лидерборд
            driver.Navigate().GoToUrl("https://chub.ai/leaderboard?segment=followers");
            wait.Until(d => d.FindElements(By.CssSelector("main table tbody tr")).Count > 1);

            var rowsAll    = driver.FindElements(By.CssSelector("main table tbody tr")).ToList();
            var mainWindow = driver.CurrentWindowHandle;

            Console.WriteLine($"Найдено пользователей: {rowsAll.Count}");

            // Обработка батчами по 10 пользователей
            for (int i = 0; i < rowsAll.Count; i += BatchSize)
            {
                var batch = rowsAll.Skip(i).Take(BatchSize).ToList();
                Console.WriteLine($"\n--- БАТЧ {i+1}–{i+batch.Count} из {rowsAll.Count} ---");

                foreach (var row in batch)
                {
                    try
                    {
                        var linkElem = row.FindElement(By.CssSelector("td:nth-child(2) a"));
                        var rawName  = linkElem.Text.Trim().TrimStart('@');
                        var userUrl  = linkElem.GetAttribute("href");
                        Console.WriteLine($"\n[User {rowsAll.IndexOf(row)+1}/{rowsAll.Count}] {rawName}");

                        // Папка пользователя
                        var userDir = Path.Combine(root, rawName);
                        Directory.CreateDirectory(userDir);

                        // Открываем профиль
                        ((IJavaScriptExecutor)driver)
                            .ExecuteScript($"window.open('{userUrl}', '_blank');");
                        Thread.Sleep(OpenPauseMs);
                        driver.SwitchTo().Window(driver.WindowHandles.Last());

                        // Скачиваем все персонажи со всех страниц
                        DownloadAllCharacters(driver, wait, userDir, downloadPath);

                        // Закрываем вкладку и возвращаемся
                        driver.Close();
                        driver.SwitchTo().Window(mainWindow);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR user] {ex.Message}");
                        // Пытаемся вернуться к главному окну
                        try
                        {
                            driver.SwitchTo().Window(mainWindow);
                        }
                        catch { }
                    }
                }

                if (i + BatchSize < rowsAll.Count) // Если это не последний батч
                {
                    Console.WriteLine($"--- пауза {BatchPauseMs} мс ---");
                    Thread.Sleep(BatchPauseMs);
                }
            }

            Console.WriteLine("\n=== ВСЕ ПОЛЬЗОВАТЕЛИ ОБРАБОТАНЫ ===");
            Console.ReadLine();
            driver.Quit();
        }

        static void DownloadAllCharacters(IWebDriver driver, WebDriverWait wait, string userDir, string tempDownloadPath)
        {
            var actions = new Actions(driver);

            // Переключаемся на вкладку Characters
            var charTab = driver.FindElements(By.CssSelector("div[role='tab']"))
                               .FirstOrDefault(t => t.Text.Trim().Equals("Characters", StringComparison.OrdinalIgnoreCase));
            charTab?.Click();
            
            // Ждём загрузки персонажей с коротким таймаутом
            try
            {
                wait.Until(d => d.FindElements(By.CssSelector("#chara-list > a.cursor-pointer")).Count > 0);
            }
            catch
            {
                Console.WriteLine("  Нет персонажей у пользователя");
                return;
            }

            int pageNum = 1;
            int totalDownloaded = 0;
            bool hasNext = true;
            
            while (hasNext)
            {
                // Собираем href всех карточек на текущей странице
                var cards = driver.FindElements(By.CssSelector("#chara-list > a.cursor-pointer")).ToList();
                var hrefs = cards.Select(a => a.GetAttribute("href")).ToList();
                
                Console.WriteLine($"  Страница {pageNum}: {hrefs.Count} персонажей");

                // Быстрая проверка - сколько уже скачано
                int alreadyExists = 0;
                var toDownload = new List<(string href, string id, int index)>();
                
                for (int idx = 0; idx < hrefs.Count; idx++)
                {
                    var href = hrefs[idx];
                    var id   = href.TrimEnd('/').Split('/').Last();
                    var png  = Path.Combine(userDir, id + ".png");
                    
                    if (File.Exists(png))
                        alreadyExists++;
                    else
                        toDownload.Add((href, id, idx));
                }
                
                if (alreadyExists > 0)
                    Console.WriteLine($"    Уже скачано: {alreadyExists}, нужно скачать: {toDownload.Count}");

                // Скачиваем только отсутствующие
                foreach (var (href, id, idx) in toDownload)
                {
                    Console.Write($"    [{idx+1}/{hrefs.Count}] {id}… ");

                    var profileHandle = driver.CurrentWindowHandle;
                    
                    try
                    {
                        // Открываем detail-вкладку
                        ((IJavaScriptExecutor)driver).ExecuteScript($"window.open('{href}', '_blank');");
                        Thread.Sleep(OpenPauseMs);
                        
                        var detailHandle = driver.WindowHandles.Except(new[] { profileHandle }).Last();
                        driver.SwitchTo().Window(detailHandle);

                        // Ждем полной загрузки страницы персонажа
                        Thread.Sleep(PageLoadWaitMs);
                        
                        // Ждем появления картинки персонажа
                        try
                        {
                            wait.Until(d => d.FindElement(By.CssSelector("img.object-cover")).Displayed);
                        }
                        catch { }

                        // Ищем кнопку Download и ждем, пока она станет кликабельной
                        var downloadBtn = wait.Until(d =>
                        {
                            var btn = d.FindElement(By.XPath(
                                "//*[@id='root']/div/div/div/main/div/div[1]/div[1]/div[2]/div/button[1]"));
                            return (btn.Displayed && btn.Enabled) ? btn : null;
                        });
                        
                        if (downloadBtn != null)
                        {
                            // Очищаем папку загрузок перед скачиванием
                            ClearTempDownloads(tempDownloadPath);
                            
                            downloadBtn.Click();
                            
                            // Ждем появления файла
                            var downloadSuccess = WaitForDownload(tempDownloadPath, id, userDir);
                            
                            if (downloadSuccess)
                            {
                                Console.WriteLine("OK");
                                totalDownloaded++;
                            }
                            else
                            {
                                Console.WriteLine("таймаут загрузки");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ошибка: {ex.Message.Split('\n')[0]}");
                    }
                    finally
                    {
                        driver.Close();
                        driver.SwitchTo().Window(profileHandle);
                    }
                }

                // Пагинация: проверяем наличие кнопки Next Page
                try
                {
                    Thread.Sleep(300); // Короткая пауза
                    
                    var nextButton = driver.FindElements(By.XPath("//*[@id='rc-tabs-1-panel-characters']/ul[1]/li[@title='Next Page']"))
                                          .FirstOrDefault() ??
                                    driver.FindElements(By.CssSelector(".ant-pagination-next[title='Next Page']"))
                                          .FirstOrDefault();
                    
                    if (nextButton != null && nextButton.GetAttribute("aria-disabled") != "true")
                    {
                        var firstHrefBefore = hrefs[0];
                        
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", nextButton);
                        
                        // Ждем обновления списка персонажей
                        Thread.Sleep(PageTransitionMs);
                        
                        try
                        {
                            wait.Until(d =>
                            {
                                var newCards = d.FindElements(By.CssSelector("#chara-list > a.cursor-pointer"));
                                return newCards.Count > 0 && 
                                       newCards[0].GetAttribute("href") != firstHrefBefore;
                            });
                        }
                        catch { }
                        
                        pageNum++;
                    }
                    else
                    {
                        hasNext = false;
                    }
                }
                catch
                {
                    hasNext = false;
                }
            }
            
            Console.WriteLine($"  → Обработано страниц: {pageNum}, скачано файлов: {totalDownloaded}");
        }
        
        static void ClearTempDownloads(string tempDownloadPath)
        {
            try
            {
                // Удаляем старые PNG файлы из временной папки
                var oldFiles = Directory.GetFiles(tempDownloadPath, "*.png")
                                      .Where(f => File.GetCreationTime(f) < DateTime.Now.AddMinutes(-5));
                foreach (var file in oldFiles)
                {
                    try { File.Delete(file); } catch { }
                }
            }
            catch { }
        }
        
        static bool WaitForDownload(string tempDownloadPath, string characterId, string userDir)
        {
            try
            {
                // Ждем появления нового файла до 5 секунд
                for (int i = 0; i < 50; i++) // 50 * 100ms = 5 секунд
                {
                    var files = Directory.GetFiles(tempDownloadPath, "*.png")
                                        .Select(f => new FileInfo(f))
                                        .Where(f => f.CreationTime > DateTime.Now.AddSeconds(-10))
                                        .OrderByDescending(f => f.CreationTime)
                                        .ToList();
                    
                    if (files.Any())
                    {
                        var sourceFile = files.First();
                        
                        // Проверяем, что файл не растет (загрузка завершена)
                        var size1 = sourceFile.Length;
                        Thread.Sleep(100);
                        sourceFile.Refresh();
                        var size2 = sourceFile.Length;
                        
                        if (size1 == size2 && size1 > 0)
                        {
                            var destFile = Path.Combine(userDir, characterId + ".png");
                            File.Move(sourceFile.FullName, destFile, true);
                            return true;
                        }
                    }
                    
                    Thread.Sleep(100);
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
