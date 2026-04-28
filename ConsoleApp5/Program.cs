using System;
using System.Collections.Generic;

// STATE

public interface IDocumentState
{
    void Print(Document document);
    void AddToQueue(Document document);
    void CompletePrinting(Document document);
    void FailPrinting(Document document);
    void Reset(Document document);
}

public class NewState : IDocumentState
{
    public void Print(Document document)
    {
        Console.WriteLine("[State: New] Документ ещё не в очереди. Сначала добавьте его в очередь.");
    }

    public void AddToQueue(Document document)
    {
        Console.WriteLine("[State: New] Запрос на добавление в очередь.");
        document.Mediator.Notify(document, "AddToQueue", document);
    }

    public void CompletePrinting(Document document)
    {
        Console.WriteLine("[State: New] Нельзя завершить печать, документ не печатается.");
    }

    public void FailPrinting(Document document)
    {
        Console.WriteLine("[State: New] Нельзя зафиксировать ошибку, документ не печатается.");
    }

    public void Reset(Document document)
    {
        Console.WriteLine("[State: New] Документ уже в состоянии New.");
    }
}

public class PrintingState : IDocumentState
{
    public void Print(Document document)
    {
        Console.WriteLine("[State: Printing] Документ уже печатается, повторная печать невозможна.");
    }

    public void AddToQueue(Document document)
    {
        Console.WriteLine("[State: Printing] Нельзя добавить в очередь во время печати.");
    }

    public void CompletePrinting(Document document)
    {
        Console.WriteLine("[State: Printing] Печать успешно завершена -> переходим в Done.");
        document.SetState(new DoneState());
    }

    public void FailPrinting(Document document)
    {
        Console.WriteLine("[State: Printing] Ошибка во время печати -> переходим в Error.");
        document.SetState(new ErrorState());
    }

    public void Reset(Document document)
    {
        Console.WriteLine("[State: Printing] Нельзя сбросить документ во время печати.");
    }
}

public class DoneState : IDocumentState
{
    public void Print(Document document)
    {
        Console.WriteLine("[State: Done] Документ уже напечатан, печать невозможна.");
    }

    public void AddToQueue(Document document)
    {
        Console.WriteLine("[State: Done] Нельзя добавить напечатанный документ в очередь.");
    }

    public void CompletePrinting(Document document)
    {
        Console.WriteLine("[State: Done] Документ уже завершён.");
    }

    public void FailPrinting(Document document)
    {
        Console.WriteLine("[State: Done] Нельзя зафиксировать ошибку для завершённого документа.");
    }

    public void Reset(Document document)
    {
        Console.WriteLine("[State: Done] Нельзя сбросить напечатанный документ.");
    }
}

public class ErrorState : IDocumentState
{
    public void Print(Document document)
    {
        Console.WriteLine("[State: Error] Печать невозможна из-за ошибки. Сначала сбросьте документ (Reset).");
    }

    public void AddToQueue(Document document)
    {
        Console.WriteLine("[State: Error] Нельзя добавить в очередь документ с ошибкой. Сбросьте его.");
    }

    public void CompletePrinting(Document document)
    {
        Console.WriteLine("[State: Error] Нельзя завершить печать, документ в ошибке.");
    }

    public void FailPrinting(Document document)
    {
        Console.WriteLine("[State: Error] Документ уже в состоянии ошибки.");
    }

    public void Reset(Document document)
    {
        Console.WriteLine("[State: Error] Сброс документа -> переход в New.");
        document.SetState(new NewState());
    }
}

// MEDIATOR

public interface IMediator
{
    void Notify(Colleague sender, string ev, Document document = null);
}

public abstract class Colleague
{
    public IMediator Mediator { get; private set; }

    public void SetMediator(IMediator mediator)
    {
        Mediator = mediator;
    }
}


public class Document : Colleague
{
    private IDocumentState _state;
    public string Title { get; }

    public Document(string title)
    {
        Title = title;
        _state = new NewState();
        Console.WriteLine($"[Документ] Создан документ '{title}' в состоянии New.");
    }

    public void SetState(IDocumentState state) => _state = state;

    public void Print() => _state.Print(this);
    public void AddToQueue() => _state.AddToQueue(this);
    public void CompletePrinting() => _state.CompletePrinting(this);
    public void FailPrinting() => _state.FailPrinting(this);
    public void Reset() => _state.Reset(this);
}

public class Printer : Colleague
{
    public bool SimulateFailure { get; set; } = false;

    public void StartPrint(Document document)
    {
        Console.WriteLine($"[Принтер] Начало физической печати '{document.Title}'...");
        if (SimulateFailure)
        {
            Console.WriteLine("[Принтер] ПРОИЗОШЛА ОШИБКА!");
            SimulateFailure = false;
            Mediator.Notify(this, "PrintFailed", document);
        }
        else
        {
            Console.WriteLine("[Принтер] Печать успешно завершена.");
            Mediator.Notify(this, "PrintSuccess", document);
        }
    }
}

public class PrintQueue : Colleague
{
    private Queue<Document> _queue = new Queue<Document>();

    public void EnqueueItem(Document document)
    {
        _queue.Enqueue(document);
        Console.WriteLine($"[Очередь] Документ '{document.Title}' добавлен в очередь (всего {_queue.Count}).");
        Mediator.Notify(this, "Enqueued", document);
    }

    public Document DequeueItem()
    {
        return _queue.Dequeue();
    }

    public bool IsEmpty => _queue.Count == 0;
}

public class Logger : Colleague
{
    public void WriteMessage(string message)
    {
        Console.WriteLine($"[LOG] {DateTime.Now:mm:ss} - {message}");
    }
}

public class Dispatcher : Colleague
{
    public void AddDocument(Document document)
    {
        Console.WriteLine($"[Диспетчер] Команда: добавить документ '{document.Title}'.");
        Mediator.Notify(this, "DispatchAdd", document);
    }

    public void CommandProcessQueue()
    {
        Console.WriteLine("[Диспетчер] Команда: обработать очередь печати.");
        Mediator.Notify(this, "ProcessQueue");
    }

    public void ResetDocument(Document document)
    {
        Console.WriteLine($"[Диспетчер] Команда: сбросить документ '{document.Title}'.");
        Mediator.Notify(this, "ResetDocument", document);
    }
}

public class PrintSystemMediator : IMediator
{
    private readonly Printer _printer;
    private readonly PrintQueue _queue;
    private readonly Logger _logger;
    private readonly Dispatcher _dispatcher;

    public PrintSystemMediator(Printer printer, PrintQueue queue, Logger logger, Dispatcher dispatcher)
    {
        _printer = printer;
        _queue = queue;
        _logger = logger;
        _dispatcher = dispatcher;

        _printer.SetMediator(this);
        _queue.SetMediator(this);
        _logger.SetMediator(this);
        _dispatcher.SetMediator(this);
    }

    public void Notify(Colleague sender, string ev, Document document = null)
    {
        switch (ev)
        {
            case "DispatchAdd":
                document?.AddToQueue();
                break;

            case "AddToQueue":
                if (document != null)
                    _queue.EnqueueItem(document);
                break;

            case "Enqueued":
                _logger.WriteMessage($"Документ '{document?.Title}' помещён в очередь.");
                break;

            case "ProcessQueue":
                if (_queue.IsEmpty)
                {
                    _logger.WriteMessage("Очередь печати пуста.");
                    return;
                }
                var nextDoc = _queue.DequeueItem();
                _logger.WriteMessage($"Начинаем обработку документа '{nextDoc.Title}' из очереди.");
                nextDoc.Print();
                break;

            case "RequestPrint":
                _logger.WriteMessage($"Получен запрос на печать документа '{document?.Title}'.");
                document.SetState(new PrintingState());
                _printer.StartPrint(document);
                break;

            case "PrintSuccess":
                _logger.WriteMessage($"Печать документа '{document?.Title}' прошла УСПЕШНО.");
                document.CompletePrinting();
                break;

            case "PrintFailed":
                _logger.WriteMessage($"Печать документа '{document?.Title}' завершилась ОШИБКОЙ.");
                document.FailPrinting();
                break;

            case "ResetDocument":
                if (document != null)
                {
                    _logger.WriteMessage($"Запрошен сброс документа '{document.Title}'.");
                    document.Reset();
                }
                break;

            default:
                _logger.WriteMessage($"Неизвестное событие: {ev}");
                break;
        }
    }
}

class Program
{
    static void Main(string[] args)
    {

        var printer = new Printer();
        var queue = new PrintQueue();
        var logger = new Logger();
        var dispatcher = new Dispatcher();

        var mediator = new PrintSystemMediator(printer, queue, logger, dispatcher);

        var doc1 = new Document("Отчёт по продажам");
        var doc2 = new Document("Договор поставки");
        var doc3 = new Document("Презентация проекта");

        doc1.SetMediator(mediator);
        doc2.SetMediator(mediator);
        doc3.SetMediator(mediator);


        Console.WriteLine("\n1: Успешная печать двух документов");
        dispatcher.AddDocument(doc1);
        dispatcher.AddDocument(doc2);
        dispatcher.CommandProcessQueue();
        dispatcher.CommandProcessQueue();

        Console.WriteLine("\n2: Ошибка принтера при печати");
        var docError = new Document("Важный контракт");
        docError.SetMediator(mediator);
        dispatcher.AddDocument(docError);

        printer.SimulateFailure = true;
        dispatcher.CommandProcessQueue();

        Console.WriteLine("\n3: Восстановление после ошибки");
        dispatcher.ResetDocument(docError);
        dispatcher.AddDocument(docError);
        dispatcher.CommandProcessQueue();

        Console.WriteLine("\n4: Попытка печати уже напечатанного документа");
        doc1.AddToQueue();

    }
}
