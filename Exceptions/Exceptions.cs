namespace Estoque.Exceptions;

public class NaoEncontradoException(string message) : Exception(message);
public class NegocioException(string message) : Exception(message);
public class ConflitoException(string message) : Exception(message);