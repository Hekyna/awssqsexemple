using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using awssqsexemple.Models;
using System.Text.Json;

var sqs = new AmazonSQSClient("", "", RegionEndpoint.EUCentral1);
var url = "";
Console.OutputEncoding = System.Text.Encoding.UTF8;

while (true)
{
    Console.WriteLine("\n1 - Створити заявку");
    Console.WriteLine("2 - Обробити всі заявки");
    Console.WriteLine("3 - Показати приблизну кількість заявок у черзі");
    Console.WriteLine("0 - Вихід");
    Console.Write("Вибери пункт: ");

    var choice = Console.ReadLine();

    if (choice == "0")
    {
        break;
    }

    if (choice == "1")
    {
        var req = new Supporttttt();

        req.Id = Guid.NewGuid();

        Console.Write("Введи ім'я: ");
        req.UserName = Console.ReadLine();

        Console.Write("Введи тему: ");
        req.Topic = Console.ReadLine();

        Console.Write("Введи опис: ");
        req.Description = Console.ReadLine();

        Console.Write("Введи пріоритет (Low, Medium, High): ");
        req.Priority = Console.ReadLine();

        req.CreatedAt = DateTime.UtcNow;

        var json = JsonSerializer.Serialize(req);

        await sqs.SendMessageAsync(url, json);

        Console.WriteLine($"Заявка №{req.Id} додана в чергу!");
    }
    else if (choice == "2")
    {
        ReceiveMessageResponse resp;

        do
        {
            resp = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest()
            {
                QueueUrl = url,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 5
            });

            if (resp.Messages == null || resp.Messages.Count == 0)
            {
                Console.WriteLine("Черга порожня.");
                break;
            }

            foreach (var m in resp.Messages)
            {
                var r = JsonSerializer.Deserialize<Supporttttt>(m.Body);

                var waitTime = DateTime.UtcNow - r.CreatedAt;

                Console.WriteLine($"\n[Заявка #{r.Id}]");
                Console.WriteLine($"Клієнт: {r.UserName} | Тема: {r.Topic}");
                Console.WriteLine($"Опис: {r.Description}");
                Console.WriteLine($"Пріоритет: {r.Priority}");
                Console.WriteLine($"Чекала в черзі: {waitTime.TotalSeconds:F1} сек.");

                if (r.Priority == "High")
                {
                    Console.WriteLine("Термінова заявка");
                }

                await sqs.DeleteMessageAsync(url, m.ReceiptHandle);
            }

        } while (resp.Messages.Count != 0);
    }
    else if (choice == "3")
    {
        var attr = await sqs.GetQueueAttributesAsync(
            url,
            new List<string> { "ApproximateNumberOfMessages" });

        Console.WriteLine($"Приблизна кількість заявок: {attr.ApproximateNumberOfMessages}");
    }
}