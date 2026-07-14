-- Aprovar usuário para login (status 2 = Aprovado)
-- Execute no banco imperial_colors

SELECT username, email, status, permissao FROM usuarios;

-- Aprovar um usuário específico:
UPDATE usuarios SET status = 2 WHERE username = 'teste';

-- Tornar usuário administrador (permissao 1 = Admin):
-- UPDATE usuarios SET status = 2, permissao = 1 WHERE username = 'teste';
